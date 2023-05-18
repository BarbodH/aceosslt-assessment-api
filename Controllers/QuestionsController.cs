using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AceOSSLT_AssessmentAPI.Models;
using AceOSSLT_AssessmentAPI.Data;
using AceOSSLT_DataTransferObjects; // Custom NuGet package containing DTO classes

namespace AceOSSLT_AssessmentAPI.Controllers
{
    /// <summary>
    /// Controller for the AceOSSLT application's RESTful API.
    /// This API controller is responsible for handling the necessary CRUD operations
    /// associated with the Question database model.
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class QuestionsController : ControllerBase
	{
        private readonly DataContext _db;

        /// <summary>
        /// API controller constructor initializing internal properties using
        /// dependency injection. The parameter are provided from the services
        /// added to the builder container.
        /// </summary>
        /// <param name="db">Database context</param>
        public QuestionsController(DataContext db)
		{
            _db = db;
		}

        /// <summary>
        /// POST endpoint for adding an assessment question to the database.
        /// </summary>
        /// <param name="questionDTO">Question data transfer object (DTO)</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateQuestion([FromBody] QuestionDTO questionDTO)
        {
            /* Server-side validation */
            // Null, empty, or out of bounds entries
            if (questionDTO.AssessmentName == null || questionDTO.AssessmentName.Length == 0 ||
                questionDTO.Text == null || questionDTO.Text.Length == 0)
                return BadRequest("The provided question properties cannot be null or empty");
            if (questionDTO.Options == null || questionDTO.Options.Count != 4)
                return BadRequest("The question must contain exactly 4 options.");
            if (questionDTO.AnswerIndex > 3 || questionDTO.AnswerIndex < 0)
                return BadRequest("The answer index must be within [0, 3] range.");
            // Invalid parent assessment name
            var parentAssessment = _db.Assessments
                .Include(a => a.Questions)
                .FirstOrDefault(a =>
                    a.Name.ToLower().Equals(questionDTO.AssessmentName.ToLower()));
            if (parentAssessment == null)
                return BadRequest($"There is no assessment named '{questionDTO.AssessmentName}' (case insensitive).");
            // Duplicate question text, i.e., question text must be unique within the question
            var duplicateQuestion = parentAssessment.Questions.FirstOrDefault(q =>
                q.Text.ToLower().Equals(questionDTO.Text.ToLower()));
            if (duplicateQuestion != null)
                return BadRequest("A question with the same text already exists (case insensitive).");

            /* Initialize new Question class instance */
            var newQuestion = new Question()
            {
                Assessment = parentAssessment,
                Text = questionDTO.Text
            };
            for (int i = 0; i < questionDTO.Options.Count; i++)
            {
                var newOption = new Option()
                {
                    Question = newQuestion,
                    Text = questionDTO.Options[i],
                    isCorrect = i == questionDTO.AnswerIndex
                };
                await _db.Options.AddAsync(newOption);
            }

            /* Update the database */
            await _db.Questions.AddAsync(newQuestion);
            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// DELETE endpoint for removing an assessment from the database using unique assessment name
        /// and question text.
        /// </summary>
        /// <param name="questionDTO">Question data transfer object (DTO)</param>
        /// <returns></returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteQuestion(QuestionDTO questionDTO)
        {
            /* Server-side validation */
            // Null or empty entries
            if (questionDTO.AssessmentName == null || questionDTO.AssessmentName.Length == 0 ||
                questionDTO.Text == null || questionDTO.Text.Length == 0)
                return BadRequest("Assessment name and question text must be provided.");
            // Invalid assessment name
            var parentAssessment = _db.Assessments
                .Include(a => a.Questions)
                .FirstOrDefault(a =>
                    a.Name.ToLower().Equals(questionDTO.AssessmentName));
            if (parentAssessment == null)
                return BadRequest($"There is no assessment named '{questionDTO.AssessmentName}' (case insensitive).");
            // Invalid question text
            var targetQuestion = parentAssessment.Questions.FirstOrDefault(q =>
                q.Text.ToLower().Equals(questionDTO.Text.ToLower()));
            if (targetQuestion == null)
                return BadRequest($"There is no question with the provided text (case insensitive).");

            /* Delete target instance and update the database */
            _db.Questions.Remove(targetQuestion);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

