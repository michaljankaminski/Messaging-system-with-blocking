# Messaging-system-with-blocking

## Przygotowanie środowiska 
Do poprawnego działania wymagana jest baza danych PSQL i broker RabbitMQ. Zalecana jest konteneryzacja i instalacja przy użyciu poniższych komend:
- PostgreSQL 
 `docker run --name edcs-postgres -p 8082:5432 -e POSTGRES_PASSWORD=postgres -d postgres`
- RabbitMQ 
 `docker run -d -p 8080:15672 -p 8081:5672 rabbitmq:3-management`
