using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AceOSSLT_AssessmentAPI.Models
{
    public class Assessment
    {
        // Identity column
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Behavioural properties
        public string Type { get; set; }
        public string Name { get; set; }

        // 1:N relationship -> Question
        public List<Question> Questions { get; set; }

        // 1:1 relationship -> Passage
        public Passage Passage { get; set; }
    }
}
