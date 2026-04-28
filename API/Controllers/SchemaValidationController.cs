using BLL.Services;
using DAL.Repositories.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/schema-validation")]
    public class SchemaValidationController : ControllerBase
    {
        private readonly ISchemaValidationService _schemaValidation;
        private readonly IScriptRepository _scriptRepository;

        public SchemaValidationController(
            ISchemaValidationService schemaValidation,
            IScriptRepository scriptRepository)
        {
            _schemaValidation = schemaValidation;
            _scriptRepository = scriptRepository;
        }

        /// <summary>
        /// SQL metnini doğrudan göndererek şema doğrulaması yapar.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ValidateByText(
            [FromBody] SchemaValidationRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest(new { message = "Geçersiz istek." });

            if (request.TargetEnvironmentId <= 0)
                return BadRequest(new { message = "Geçerli bir hedef ortam ID'si belirtilmelidir." });

            var result = await _schemaValidation.ValidateAsync(
                request.SqlScript,
                request.RollbackScript,
                request.TargetEnvironmentId,
                cancellationToken);

            return Ok(MapResult(result));
        }

        /// <summary>
        /// Kayıtlı bir script'i, seçilen hedef ortama karşı doğrular.
        /// </summary>
        [HttpPost("by-script/{scriptId:long}")]
        public async Task<IActionResult> ValidateByScript(
            long scriptId,
            [FromQuery] long targetEnvironmentId,
            CancellationToken cancellationToken)
        {
            if (scriptId <= 0)
                return BadRequest(new { message = "Geçersiz script ID'si." });

            if (targetEnvironmentId <= 0)
                return BadRequest(new { message = "Geçerli bir hedef ortam ID'si belirtilmelidir." });

            var script = await _scriptRepository.GetByIdAsync(scriptId);
            if (script == null)
                return NotFound(new { message = $"Script bulunamadı (ID: {scriptId})." });

            var result = await _schemaValidation.ValidateAsync(
                script.SqlScript,
                script.RollbackScript,
                targetEnvironmentId,
                cancellationToken);

            return Ok(new
            {
                scriptId = script.Id,
                scriptName = script.Name,
                validation = MapResult(result)
            });
        }

        private static object MapResult(SchemaValidationResult result) => new
        {
            isValid = result.IsValid,
            errorMessage = result.ErrorMessage,
            tables = result.Tables.Select(t => new
            {
                tableName = t.TableName,
                existsInDatabase = t.ExistsInDatabase,
                scriptColumns = t.ScriptColumns.Select(c => new
                {
                    columnName = c.ColumnName,
                    existsInDatabase = c.ExistsInDatabase,
                    actualDataType = c.ActualDataType
                }),
                databaseColumns = t.DatabaseColumns
            })
        };
    }

    public sealed class SchemaValidationRequest
    {
        public string? SqlScript { get; set; }
        public string? RollbackScript { get; set; }
        public long TargetEnvironmentId { get; set; }
    }
}
