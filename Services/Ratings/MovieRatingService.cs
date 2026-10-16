using MovieStreaming.DTOs.Ratings;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;
using MovieStreaming.Services.Authentication;

namespace MovieStreaming.Services.Ratings;

public class MovieRatingService : IMovieRatingService
{
    private readonly IMovieRatingRepository _movieRatingRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly ICurrentUserService _currentUserService;

    public MovieRatingService(
        IMovieRatingRepository movieRatingRepository,
        IMovieRepository movieRepository,
        ICurrentUserService currentUserService)
    {
        _movieRatingRepository = movieRatingRepository;
        _movieRepository = movieRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RatingDto> RateMovieAsync(
        int movieId,
        CreateRatingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        if (request == null || request.Rating < 1 || request.Rating > 10)
        {
            throw new ValidationException("Rating must be an integer between 1 and 10.");
        }

        var movie = await _movieRepository.GetMovieByIdAsync(movieId, cancellationToken);
        if (movie == null)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var userId = _currentUserService.UserId;
        var existing = await _movieRatingRepository.GetByUserAndMovieIdAsync(userId, movieId, cancellationToken);

        if (existing != null)
        {
            existing.Rating = request.Rating;
            existing.UpdatedAt = DateTime.UtcNow;
            await _movieRatingRepository.UpdateAsync(existing, cancellationToken);
            return MapToRatingDto(existing);
        }

        var rating = new MovieRating
        {
            UserId = userId,
            MovieId = movieId,
            Rating = request.Rating,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _movieRatingRepository.AddAsync(rating, cancellationToken);
        return MapToRatingDto(created);
    }

    public async Task<CurrentUserRatingDto> GetCurrentUserRatingAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        var movie = await _movieRepository.GetMovieByIdAsync(movieId, cancellationToken);
        if (movie == null)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var userId = _currentUserService.UserId;
        var rating = await _movieRatingRepository.GetByUserAndMovieIdAsync(userId, movieId, cancellationToken);

        return new CurrentUserRatingDto
        {
            MovieId = movieId,
            Rating = rating?.Rating
        };
    }

    public async Task<bool> DeleteRatingAsync(int movieId, CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        var movie = await _movieRepository.GetMovieByIdAsync(movieId, cancellationToken);
        if (movie == null)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var userId = _currentUserService.UserId;
        var deleted = await _movieRatingRepository.DeleteByUserAndMovieIdAsync(userId, movieId, cancellationToken);

        if (!deleted)
        {
            throw new NotFoundException($"Rating for movie ID {movieId} not found.");
        }

        return true;
    }

    public async Task<RatingSummaryDto> GetRatingSummaryAsync(int movieId, CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        var movie = await _movieRepository.GetMovieByIdAsync(movieId, cancellationToken);
        if (movie == null)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var (avg, count, dist) = await _movieRatingRepository.GetRatingSummaryAsync(movieId, cancellationToken);

        return new RatingSummaryDto
        {
            MovieId = movieId,
            AverageRating = avg,
            TotalRatings = count,
            Distribution = dist.ToDictionary(k => k.Key.ToString(), v => v.Value)
        };
    }

    private static RatingDto MapToRatingDto(MovieRating r)
    {
        return new RatingDto
        {
            Id = r.Id,
            MovieId = r.MovieId,
            UserId = r.UserId,
            Rating = r.Rating,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}
