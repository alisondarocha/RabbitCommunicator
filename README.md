# 🐰 RabbitCommunicator

**RabbitCommunicator** is a .NET solution that demonstrates the use of RabbitMQ for asynchronous communication between services. The project includes an API for sending messages and a Worker that consumes and processes those messages, making it ideal for event-driven systems and microservice architectures.

---

## 📂 Solution Structure

```bash
RabbitCommunicator.sln
└── src/
    ├── RabbitComunicator.Api        # REST API for publishing messages to RabbitMQ.
    └── RabbitComunicator.Worker     # Worker service that listens to and processes messages from the queue.

