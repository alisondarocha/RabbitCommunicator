# 🐰 RabbitCommunicator

**RabbitCommunicator** é uma solução .NET que demonstra a utilização do RabbitMQ para comunicação assíncrona entre serviços. O projeto inclui uma API para envio de mensagens e um Worker que consome e processa essas mensagens, sendo ideal para sistemas orientados a eventos e arquitetura de microserviços.

---

## 📂 Estrutura da Solução

```bash
RabbitCommunicator.sln
└── src/
    ├── RabbitComunicator.Api        # API REST para publicação de mensagens no RabbitMQ.
    └── RabbitComunicator.Worker     # Serviço Worker que escuta e processa mensagens da fila.
