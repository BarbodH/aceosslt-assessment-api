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
    /// This API is responsible for smoothly handling CRUD (create, read, update, delete)
    /// operations concerning AceOSSLT's assessment database.
    /// </summary>
	[Route("api/[controller]")]
    [ApiController]
    public class AssessmentController : ControllerBase
    {
        private readonly DataContext _db;

        /// <summary>
        /// API controller constructor initializing internal properties using
        /// dependency injection. The parameter are provided from the services
        /// added to the builder container.
        /// </summary>
        /// <param name="db">Database context</param>
		public AssessmentController(DataContext db)
        {
            _db = db;
        }

        /// <summary>
        /// GET endpoint for retrieving an assessment from the database.
        /// </summary>
        /// <param name="name">Assessment name, which is unique within the database</param>
        /// <returns>Assessment data transfer object (DTO)</returns>
        [HttpGet("Assessment/Get/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Assessment> GetAssessment(string name)
        {
            // Server-side validation
            if (_db.Assessments.FirstOrDefault(a => a.Name.ToLower().Equals(name.ToLower())) == null)
                return NotFound($"There is no assessment named '{name}' (case insensitive).");

            return Ok(_db.Assessments
                .Include(a => a.Questions)
                    .ThenInclude(q => q.Options)
                .Include(a => a.Passage)
                .FirstOrDefault(a => a.Name.ToLower().Equals(name.ToLower())));
        }

        /// <summary>
        /// GET endpoint for retrieving a list of assessments.
        /// </summary>
        /// <param name="type">Assesment type: 0 -> reading, 1 -> writing</param>
        /// <returns>String list of assessment names</returns>
        [HttpGet("Assessment/Get/{type:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<List<string>> GetAssessments(int type)
        {
            // Server-side validation
            if (type < 0 || type > 1)
                return BadRequest("Assessment type parameter must be either 0 (reading) or 1 (writing).");

            return Ok(_db.Assessments
                .Where(a => a.Type.Equals(type == 0 ? "Reading" : "Writing"))
                .Select(a => a.Name)
                .ToList());
        }

        /// <summary>
        /// POST endpoint for adding an assessment to the database.
        /// </summary>
        /// <param name="assessmentDTO">Assessment data transfer object (DTO)</param>
        /// <returns></returns>
        [HttpPost("Assessment/Post")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAssessment([FromBody] AssessmentDTO assessmentDTO)
        {
            /* Server-side validation */
            // Invalid assessment type
            if (!assessmentDTO.Type.Equals("Reading", StringComparison.OrdinalIgnoreCase) &&
                !assessmentDTO.Type.Equals("Writing", StringComparison.OrdinalIgnoreCase))
                return BadRequest($"Assessment type '{assessmentDTO.Type}' is not valid. " +
                    $"Expected 'Reading' or 'Writing' (case insensitive).");
            // Duplicate assessment name, i.e., assessment name must be unique within the database
            var duplicateAssessment = _db.Assessments
                .FirstOrDefault(a => a.Name.ToLower().Equals(assessmentDTO.Name.ToLower()));
            if (duplicateAssessment != null)
                return BadRequest($"An assessment with name '{assessmentDTO.Name}' already exists (case insensitive).");

            /* Initialize new Assessment class instance */
            var newAssessment = new Assessment()
            {
                // Convert the input Type to Pascal case
                Type = $"{assessmentDTO.Type.Substring(0, 1).ToUpper()}{assessmentDTO.Type.Substring(1).ToLower()}",
                Name = assessmentDTO.Name
            };
            if (newAssessment.Type.Equals("Reading"))
            {
                var newPassage = new Passage()
                {
                    Assessment = newAssessment,
                    Title = "Default title",
                    Text = "Default text..."
                };
                await _db.Passages.AddAsync(newPassage);
            }

            /* Update the database */
            await _db.Assessments.AddAsync(newAssessment);
            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// POST endpoint for adding an assessment question to the database.
        /// </summary>
        /// <param name="questionDTO">Question data transfer object (DTO)</param>
        /// <returns></returns>
        [HttpPost("Question/Post")]
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
        /// POST endpoint for adding an assessment passage to the database.
        /// </summary>
        /// <param name="passageDTO">Passage data transfer object (DTO)</param>
        /// <returns></returns>
        [HttpPost("Passage/Post")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePassage([FromBody] PassageDTO passageDTO)
        {
            /* Server-side validation */
            // Null, empty, or out of bounds entries
            if (passageDTO.AssessmentName == null || passageDTO.AssessmentName.Length == 0 ||
                passageDTO.Title == null || passageDTO.Title.Length == 0 ||
                passageDTO.Text == null || passageDTO.Text.Length == 0)
                return BadRequest("The provided passage properties cannot be null.");
            // Invalid parent assessment name or type, i.e., only 'Reading' assessments contain passages
            var parentAssessment = _db.Assessments.FirstOrDefault(a =>
                a.Name.ToLower().Equals(passageDTO.AssessmentName.ToLower()));
            if (parentAssessment == null || parentAssessment.Type.Equals("Writing"))
                return BadRequest($"There is no assessment named '{passageDTO.AssessmentName}' (case insensitive).");

            /* Initialize new Passage class instance */
            var newPassage = new Passage()
            {
                Assessment = parentAssessment,
                Title = passageDTO.Title,
                Text = passageDTO.Text
            };

            /* Update the database */
            await _db.Passages.AddAsync(newPassage);
            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// DELETE endpoint for removing an assessment from the database using assessment name.
        /// </summary>
        /// <param name="name">Assessment name, which is unique within the database</param>
        /// <returns></returns>
        [HttpDelete("Assessment/Delete/{name}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteAssessment(string name)
        {
            /* Server-side validation */
            // Invalid assessment name
            var targetAssessment = _db.Assessments.FirstOrDefault(a => a.Name.ToLower().Equals(name.ToLower()));
            if (targetAssessment == null)
                return BadRequest($"There is no assessment named '{name}' (case insensitive).");

            /* Delete target instance and update the database */
            _db.Assessments.Remove(targetAssessment);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// DELETE endpoint for removing an assessment from the database using unique assessment name
        /// and question text.
        /// </summary>
        /// <param name="questionDTO">Question data transfer object (DTO)</param>
        /// <returns></returns>
        [HttpDelete("Question/Delete")]
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
