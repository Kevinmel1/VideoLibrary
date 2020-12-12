using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VideoLibrary1.Data;
using VideoLibrary1.Models;
using System.IO;
using Xabe.FFmpeg;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace VideoLibrary1.Controllers
{
    public class MoviesController : Controller
    {
        private readonly VideoDbContext _context;
        private readonly string _dir;
        public MoviesController(VideoDbContext context, IWebHostEnvironment dir)
        {
            _context = context;
            this._dir = dir.WebRootPath;
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            return View(await _context.Movie.ToListAsync());
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create( Movie movie)
        {
            if (ModelState.IsValid)
            {
                //Output file paths and names.
                string outputVideoPath;
                string outputVideoDirectory;
                string outputFilename;

                
                var currentFileName = movie.VideoFile.FileName;
                string currentDate = DateTime.Now.ToString("yyyy_dd_M-HH-mm-ss");
                string videoDirectory = "UploadedVideos";

                FFmpeg.SetExecutablesPath(_dir + "/ffmpeg/bin", "ffmpeg", "ffprobe");

                //Input file paths and names.
                var inputVideoPath = Path.Combine(_dir, videoDirectory, currentFileName);
                var videoFilenameNoExtension = Path.GetFileNameWithoutExtension(currentFileName);
                var videoFilenameExtension = Path.GetExtension(currentFileName);

                //Opens connection, copies file to movie object.
                using (var filestream = new FileStream(inputVideoPath, FileMode.Create, FileAccess.Write))
                {
                    await movie.VideoFile.CopyToAsync(filestream);
                }

                //Gets metadata from movie file.
                var inputVideoInfo = await FFmpeg.GetMediaInfo(inputVideoPath);

                if (!videoFilenameExtension.Equals(".mp4"))
                {
                    //If video is not of format ".mp4" convert, else return same view.
                    try
                    {

                        outputFilename = currentDate + videoFilenameNoExtension + ".mp4";
                        outputVideoPath = Path.Combine(_dir, videoDirectory, outputFilename);
                        outputVideoDirectory = Path.Combine(videoDirectory, outputFilename);

                        //Sets the settings for conversion on current movie file and saves it to new variable.
                        var videostream = inputVideoInfo.VideoStreams
                            .FirstOrDefault()
                            .SetCodec(VideoCodec.h264);

                        // Convertion. Takes "videostream" and converts input video file, to output video file format.
                        await FFmpeg.Conversions.New()
                            .AddStream(videostream)
                            .SetOutput(outputVideoPath)
                            .Start();
                    }
                    catch (Exception)
                    {

                        throw;
                    }

                    movie.FileName = outputVideoDirectory;
                }
                else
                {
                    TempData["Message"] = "Already of type '.mp4'";
                    return RedirectToAction("Create");
                }

                //Gets metadata from movie file.
                var outputVideoInfo = await FFmpeg.GetMediaInfo(outputVideoPath);

                var size = Math.Round(((decimal)(outputVideoInfo.Size / 1024) / 1024),2);
                string FileSize = Convert.ToString(size + " Mb");

                movie.Title = Path.GetFileNameWithoutExtension(currentFileName);
                movie.Duration = outputVideoInfo.Duration.StripMilliseconds();
                movie.Height = outputVideoInfo.VideoStreams.FirstOrDefault().Height;
                movie.Width = outputVideoInfo.VideoStreams.FirstOrDefault().Width;
                movie.Size = FileSize;

                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,FileName,Duration,Size,Width,Height")] Movie movie)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
            _context.Movie.Remove(movie);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.Id == id);
        }
    }

    //Extention method that removes the milliseconds in TimeSpan
    public static class TimeExtensions
    {
        public static TimeSpan StripMilliseconds(this TimeSpan time)
        {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
    }

}
