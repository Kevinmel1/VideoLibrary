using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VideoLibrary1.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public TimeSpan Duration { get; set; }
        public string Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        [NotMapped]
        public IFormFile VideoFile { get; set; }
    }
}
