# legendary-cashin

## CashinService REST API

The project under `backend/CashinService/CashinService` is now an ASP.NET Core REST API using MVC controllers and generated OpenAPI documentation.

### Endpoints

- `GET /` - service metadata
- `GET /health` - health check
- `GET /cashins` - list submitted cashins
- `GET /cashins/{id}` - fetch a single cashin by id
- `POST /cashins` - create a cashin

### API documentation

- OpenAPI JSON: `GET /openapi/v1.json`
- Scalar UI: `GET /scalar/v1`

### Sample request body

```json
{
  "amount": 150.75,
  "currency": "USD",
  "reference": "invoice-1001"
}
```

### Run locally

```fish
cd backend/CashinService/CashinService
dotnet run
```

