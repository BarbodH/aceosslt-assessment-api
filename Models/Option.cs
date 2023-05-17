using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AceOSSLT_AssessmentAPI.Models
{
    public class Option
    {
        // Identity column
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Behavioural properties
        public string Text { get; set; }
        public bool isCorrect { get; set; }

        // 1:N relationship <- Question
        public int QuestionId { get; set; }
        [JsonIgnore]
        public Question Question { get; set; }
    }
}
