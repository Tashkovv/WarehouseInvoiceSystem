namespace WarehouseInvoiceSystem.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WarehouseInvoiceSystem.Application.DTOs.Company;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Company.Enums;

    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController(ICompanyService companyService, ILogger<CompaniesController> logger) : ControllerBase
    {
        /// <summary>
        /// Get all companies
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAll()
        {
            try
            {
                IEnumerable<CompanyDto> companies = await companyService.GetAllCompaniesAsync();
                return Ok(companies);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting all companies");
                return StatusCode(500, "An error occurred while retrieving companies");
            }
        }

        /// <summary>
        /// Get companies by type (Client, Vendor, or Both)
        /// </summary>
        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetByType(CompanyType type)
        {
            try
            {
                IEnumerable<CompanyDto> companies = await companyService.GetCompaniesByTypeAsync(type);
                return Ok(companies);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting companies by type {Type}", type);
                return StatusCode(500, "An error occurred while retrieving companies");
            }
        }

        /// <summary>
        /// Get company by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CompanyDto>> GetById(int id)
        {
            try
            {
                CompanyDto? company = await companyService.GetCompanyByIdAsync(id);
                if (company == null)
                    return NotFound($"Company with ID {id} not found");

                return Ok(company);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting company {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the company");
            }
        }

        /// <summary>
        /// Get company balance (what they owe us vs what we owe them)
        /// </summary>
        [HttpGet("{id}/balance")]
        public async Task<ActionResult<CompanyBalanceDto>> GetBalance(int id)
        {
            try
            {
                CompanyBalanceDto balance = await companyService.GetCompanyBalanceAsync(id);
                return Ok(balance);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Company with ID {id} not found");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting balance for company {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the balance");
            }
        }

        /// <summary>
        /// Create a new company
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                CompanyDto company = await companyService.CreateCompanyAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = company.Id }, company);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating company");
                return StatusCode(500, "An error occurred while creating the company");
            }
        }

        /// <summary>
        /// Update an existing company
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<CompanyDto>> Update(int id, [FromBody] UpdateCompanyDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                CompanyDto company = await companyService.UpdateCompanyAsync(id, updateDto);
                return Ok(company);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Company with ID {id} not found");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating company {Id}", id);
                return StatusCode(500, "An error occurred while updating the company");
            }
        }

        /// <summary>
        /// Delete a company (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                bool result = await companyService.DeleteCompanyAsync(id);
                if (!result)
                {
                    return NotFound($"Company with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting company {Id}", id);
                return StatusCode(500, "An error occurred while deleting the company");
            }
        }
    }
}
