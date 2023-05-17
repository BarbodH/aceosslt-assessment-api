using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AceOSSLT_AssessmentAPI.Models
{
    public class Passage
    {
        // Identity column
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Behavioural properties
        public string Title { get; set; }
        public string Text { get; set; }

        // 1:1 relationship <- Assessment
        public int AssessmentId { get; set; }
        [JsonIgnore]
        public Assessment Assessment { get; set; }
    }
}
