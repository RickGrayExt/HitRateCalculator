# Hit-Rate Microservices (Completed TODOs)

- .NET 8 minimal APIs
- MassTransit + RabbitMQ
- YARP Gateway
- End-to-end flow with real calculations (PTO & PTL)

## Quick Start
```bash
docker compose up --build -d
# Start a run (PTO)
curl -X POST http://localhost:8000/runs -H "Content-Type: application/json" -d '{"datasetPath":"/data/sample_sales.csv","mode":"PTO"}'
# Or PTL
curl -X POST http://localhost:8000/runs -H "Content-Type: application/json" -d '{"datasetPath":"/data/sample_sales.csv","mode":"PTL"}'

# Poll status
curl http://localhost:8000/runs/<RUN_ID>
# Fetch result
curl http://localhost:8000/results/<RUN_ID>
```

## Data
Place your CSV in `./data`. A sample is provided as `data/sample_sales.csv` with columns:
`Order_Date,Time,Customer_Id,Product_Category,Product,Sales,Quantity,Order_Priority`


## UI
- A simple React dashboard is provided in `ui/`
- Access it at: http://localhost:3000 after `docker compose up --build`
- Use the form to start PTO/PTL runs and view results.
