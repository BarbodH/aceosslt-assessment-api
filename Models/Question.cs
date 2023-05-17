using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AceOSSLT_AssessmentAPI.Models
{
    public class Question
    {
        // Identity column
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Behavioural property
        public string Text { get; set; }

        // 1:N relationship <- Assessment
        public int AssessmentId { get; set; }
        [JsonIgnore]
        public Assessment Assessment { get; set; }

        // 1:N relationship -> Option
        public List<Option> Options { get; set; }
    }
}
