using Microsoft.AspNetCore.Mvc;

namespace WhereIsMyMoney.Api.Controllers;

[ApiController]
[Route("cashins")]
public sealed class CashinsController(CashinStore store) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<CashinResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<CashinResponse>> GetAll()
    {
        return Ok(store.GetAll());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CashinResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CashinResponse> GetById(Guid id)
    {
        var cashin = store.Get(id);

        return cashin is null
            ? NotFound(new { message = $"Cashin '{id}' was not found." })
            : Ok(cashin);
    }

    [HttpPost]
    [ProducesResponseType<CashinResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<CashinResponse> Create([FromBody] CreateCashinRequest request)
    {
        var createdCashin = store.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = createdCashin.Id }, createdCashin);
    }
}

