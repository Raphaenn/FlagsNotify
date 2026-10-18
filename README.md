# FlagsNotify

Centro de notificações em .NET 8 com Minimal API, consumidor Kafka e envio de
push pelo Expo Push Service.

## Arquitetura

- `FlagsNotify.Domain`: entidades e invariantes de domínio.
- `FlagsNotify.Application`: casos de uso e portas de saída.
- `FlagsNotify.Infrastructure`: Kafka, Expo Push e persistência PostgreSQL.
- `FlagsNotify.Api`: composição da aplicação e endpoints Minimal API.
- `tests`: testes unitários, de infraestrutura e de integração da API.

As dependências apontam para dentro: API e Infraestrutura dependem da Aplicação,
e a Aplicação depende somente do Domínio.

## Executar localmente

Pré-requisitos: .NET 8, PostgreSQL e um broker Kafka acessível. Configure o
banco `flags` sem versionar a senha:

```bash
export ConnectionStrings__PostgresRepository='Host=localhost;Port=5432;Username=admin;Password=<sua-senha>;Database=flags;'
```

```bash
dotnet restore
dotnet run --project src/FlagsNotify.Api
```

A documentação Swagger fica em `http://localhost:5187/swagger` no ambiente
`Development`. Os health checks ficam em:

- `GET /health/live`
- `GET /health/ready`

Para iniciar API, Kafka/Redpanda e criar o tópico automaticamente, mantendo o
PostgreSQL já executado na máquina host:

```bash
docker compose up --build
```

## Configuração

As chaves aceitam configuração via `appsettings.json` ou variáveis de ambiente
com `__`, por exemplo:

```bash
export Kafka__BootstrapServers=localhost:9092
export Kafka__GroupId=flags-notify
export Kafka__Topics__0=interactions-events-dev
export ExpoPush__AccessToken=seu-token-opcional
export ConnectionStrings__PostgresRepository='Host=localhost;Port=5432;Username=admin;Password=<sua-senha>;Database=flags;'
export Jwt__SigningKey='<a-mesma-chave-usada-pelo-FlagUsers>'
```

Para SASL, configure também:

```json
{
  "Kafka": {
    "SecurityProtocol": "SaslSsl",
    "SaslMechanism": "Plain",
    "SaslUsername": "usuario",
    "SaslPassword": "segredo"
  }
}
```

Nunca versionar credenciais. Em produção, injete-as por secret manager.

## PostgreSQL e migrations

Os tokens ficam na tabela `user_push_devices`, separada da tabela `users`. O
campo `user_id` mantém o vínculo lógico com o usuário e permite vários aparelhos
ativos para o mesmo usuário. A inbox fica na tabela `notifications`, com índice
por destinatário/data e controle de leitura. O FlagsNotify usa uma tabela de histórico de
migrations própria: `__flagsnotify_migrations_history`.

Em `Development`, `Database:ApplyMigrationsOnStartup` está habilitado e a API
aplica as migrations ao iniciar. Em produção ele fica desabilitado; aplique a
migration no pipeline de implantação:

```bash
dotnet tool restore
dotnet ef database update \
  --project src/FlagsNotify.Infrastructure \
  --startup-project src/FlagsNotify.Api \
  --context NotificationDbContext
```

Também é possível gerar um script idempotente para revisão e execução pelo DBA:

```bash
dotnet ef migrations script --idempotent \
  --project src/FlagsNotify.Infrastructure \
  --startup-project src/FlagsNotify.Api \
  --context NotificationDbContext
```

## Contrato inicial do Kafka

O tópico inicial é `interactions-events-dev`. Cada mensagem deve ser JSON:

```json
{
  "eventId": "interaction-01J...",
  "eventType": "HandleLike",
  "occurredAt": "2026-07-30T15:00:00Z",
  "recipientId": "user-123",
  "senderId": "user-456",
  "senderName": "Maria",
  "title": "Nova interação",
  "body": "Você recebeu uma nova mensagem.",
  "data": {
    "interactionId": "interaction-456",
    "route": "/(tabs)/(home)/home"
  }
}
```

O consumidor resolve os tokens ativos pelo `recipientId`. Mesmo sem aparelho
registrado, a notificação é persistida para aparecer na inbox do aplicativo.

O processamento é *at least once*: o offset só é confirmado depois do envio.
Uma falha transitória faz o consumidor tentar o mesmo evento novamente. Portanto,
o aplicativo cliente deve tolerar uma rara notificação duplicada.

## Registrar o token do app existente

O app Expo já pronto deve enviar o token obtido por ele:

```bash
curl -X POST http://localhost:5187/api/v1/devices \
  -H 'Content-Type: application/json' \
  -d '{
    "userId": "user-123",
    "expoPushToken": "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]"
  }'
```

O cadastro é idempotente por token: uma nova chamada reativa e atualiza o
vínculo com o usuário.

## Envio direto para teste

Este endpoint permite validar o fluxo sem publicar no Kafka:

```bash
curl -X POST http://localhost:5187/api/v1/notifications \
  -H 'Content-Type: application/json' \
  -d '{
    "eventId": "manual-test-1",
    "eventType": "HandleLike",
    "recipientUserId": "user-123",
    "senderUserId": "user-456",
    "senderUserName": "Maria",
    "title": "Teste",
    "body": "FlagsNotify está funcionando.",
    "data": {
      "route": "/(tabs)/(home)/home"
    }
  }'
```

## Inbox REST

Os endpoints da inbox exigem o JWT emitido pelo FlagUsers no header
`Authorization: Bearer <token>`:

- `GET /api/v1/notifications?cursor=...&size=20`
- `GET /api/v1/notifications/unread-count`
- `PATCH /api/v1/notifications/{notificationId}/read`
- `PATCH /api/v1/notifications/read-all`
- `DELETE /api/v1/notifications/{notificationId}`

Configure `Jwt:SigningKey`, `Jwt:Issuer` e `Jwt:Audience` com os mesmos valores
do emissor. A chave deve vir de variável de ambiente ou secret manager.

## Testes e cobertura

```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

O envio de push e o acesso ao Kafka são substituídos por doubles nos testes; a
suíte não envia notificações reais.
