using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesWithApi.Dtos;
using MoviesWithApi.Models;

namespace MoviesWithApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private new List<string> _allowedExtentions = new List<string> { ".jpg" , ".png" };
        private long _maxAllowedPosterSize = 1048576;

        public MoviesController(ApplicationDbContext context)
        {
            _context=context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var movies = await _context.Movies
                .OrderByDescending(x => x.Rate)
                .Include(m => m.Genre)
                .Select(m => new MovieDetailsDto
                {
                    Id = m.Id,
                    GenreId = m.GenreId,
                    GenreName = m.Genre.Name,
                    Poster = m.Poster,
                    Storeline = m.Storeline,
                    Title = m.Title,
                    Rate =  m.Rate,
                    Year = m.Year

                })
                .ToListAsync();
            return Ok(movies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.Genre)
                .SingleOrDefaultAsync(m => m.Id == id);

            if (movie == null)
                return NotFound();

            var dto = new MovieDetailsDto
            {
                Id = movie.Id,
                GenreId = movie.GenreId,
                GenreName = movie.Genre.Name,
                Poster = movie.Poster,
                Storeline = movie.Storeline,
                Title = movie.Title,
                Rate =  movie.Rate,
                Year = movie.Year
            };

            return Ok(dto);
        }

        [HttpGet("GetByGenreId")]
        public async Task<IActionResult> GetByGenreIdAsync(byte genreId)   
        {
            var movies = await _context.Movies
                .Where(m => m.GenreId ==  genreId)
                .OrderByDescending(x => x.Rate)
                .Include(m => m.Genre)
                .Select(m => new MovieDetailsDto
                {
                    Id = m.Id,
                    GenreId = m.GenreId,
                    GenreName = m.Genre.Name,
                    Poster = m.Poster,
                    Storeline = m.Storeline,
                    Title = m.Title,
                    Rate =  m.Rate,
                    Year = m.Year

                })
                .ToListAsync();
            return Ok(movies);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] MovieDto dto)
        {
            if (dto.Poster == null)
                return BadRequest("Poster is Required");

            if (!_allowedExtentions.Contains(Path.GetExtension(dto.Poster.FileName)))
                return BadRequest("Only .Png and .Jpg images are allowed!");
            if (dto.Poster.Length > _maxAllowedPosterSize)
                return BadRequest("Max allowed size for Poster is 1MB!");

            var isValidGenre = await _context.Genres.AnyAsync(g => g.Id == dto.GenreId);

            if (!isValidGenre)
                return BadRequest("Invalid genre ID!");

            using var datastream = new MemoryStream();

            await dto.Poster.CopyToAsync(datastream);

            var movie = new Movie
            {
                GenreId = dto.GenreId,
                Title = dto.Title,
                Poster = datastream.ToArray(),
                Rate = dto.Rate,
                Storeline = dto.Storeline,
                Year = dto.Year
            };

            await _context.AddAsync(movie);
            _context.SaveChanges();

            return Ok(movie);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromForm] MovieDto dto)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound($"No genre was found with ID: {id}");

            var isValidGenre = await _context.Genres.AnyAsync(g => g.Id == dto.GenreId);

            if (dto.Poster != null)
            {
                if (!_allowedExtentions.Contains(Path.GetExtension(dto.Poster.FileName)))
                    return BadRequest("Only .Png and .Jpg images are allowed!");
                if (dto.Poster.Length > _maxAllowedPosterSize)
                    return BadRequest("Max allowed size for Poster is 1MB!");

                using var datastream = new MemoryStream();

                await dto.Poster.CopyToAsync(datastream);

                movie.Poster = datastream.ToArray();

            }

            movie.Title = dto.Title;
            movie.Storeline = dto.Storeline;
            movie.Year = dto.Year;
            movie.GenreId = dto.GenreId;
            movie.Rate = dto.Rate;

            _context.SaveChanges();
            return Ok(movie);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound($"No genre was found with ID: {id}");

            _context.Remove(movie);
            _context.SaveChanges();
            return Ok(movie);
        }
    }
}
