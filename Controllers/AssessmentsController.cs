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
    /// associated with the Assessment database model.
    /// </summary>
	[Route("api/[controller]/[action]")]
    [ApiController]
    public class AssessmentsController : ControllerBase
    {
        private readonly DataContext _db;

        /// <summary>
        /// API controller constructor initializing internal properties using
        /// dependency injection. The parameter are provided from the services
        /// added to the builder container.
        /// </summary>
        /// <param name="db">Database context</param>
		public AssessmentsController(DataContext db)
        {
            _db = db;
        }

        /// <summary>
        /// GET endpoint for retrieving an assessment from the database.
        /// </summary>
        /// <param name="name">Assessment name, which is unique within the database</param>
        /// <returns>Assessment data transfer object (DTO)</returns>
        [HttpGet("{name}")]
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
        [HttpGet("{type:int}")]
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
        [HttpPost]
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
        /// DELETE endpoint for removing an assessment from the database using assessment name.
        /// </summary>
        /// <param name="name">Assessment name, which is unique within the database</param>
        /// <returns></returns>
        [HttpDelete("{name}")]
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
    }
}
