# 🚀 TCM | Telegram Community Migrator

Ferramenta de linha de comando desenvolvida em C# para transferência de membros entre grupos do Telegram, utilizando a API MTProto por meio da biblioteca [WTelegramClient](https://github.com/wiz0u/WTelegramClient).

## 📖 Sobre o projeto

O TCM tem como objetivo automatizar e organizar o processo de migração de comunidades entre grupos do Telegram.

A aplicação permite selecionar um grupo de origem, coletar seus participantes e utilizar esses dados em uma operação direcionada a um grupo de destino.

O projeto foi desenvolvido com uma arquitetura modular, separando a interface de terminal, a lógica de execução e os modelos de dados.

## 🔄 Fluxo de operação

```text
Grupo de origem
      |
      v
Coleta de participantes
      |
      v
Processamento dos membros
      |
      v
Grupo de destino
      |
      v
Transferência de membros elegíveis
```

## 🏗️ Arquitetura

A solução é dividida em três projetos:

```text
TCM/
├── TCM.CLI/
│   └── Interface de linha de comando
│
├── TCM.Engine/
│   └── Lógica de negócio e integração com Telegram
│
└── TCM.Destination/
    └── Modelos e estruturas de dados
```

| Projeto | Responsabilidade |
|---|---|
| `TCM.CLI` | 🖥️ Interface de terminal e interação com o usuário. |
| `TCM.Engine` | ⚙️ Comunicação com o Telegram e execução das operações. |
| `TCM.Destination` | 📦 Modelos e estruturas de dados utilizados pelo sistema. |

## ✨ Funcionalidades

- [x] 🔐 Autenticação com a API do Telegram.
- [x] 🔌 Conexão com a conta do Telegram.
- [x] 📋 Listagem de grupos disponíveis.
- [x] 🎯 Seleção de grupo de origem.
- [x] 👥 Coleta de participantes.
- [x] 📥 Seleção de grupo de destino.
- [x] 🚀 Operações de adição de membros.
- [x] 🔄 Controle de estado das operações.
- [x] 🛡️ Tratamento de falhas e restrições da API.
- [ ] 💾 Persistência do progresso das transferências.
- [ ] 📊 Relatórios detalhados de execução.

## 🛠️ Tecnologias

- **C# / .NET**: desenvolvimento da aplicação.
- **WTelegramClient**: comunicação com o Telegram via MTProto.
- **Microsoft.Extensions.Configuration**: gerenciamento de configurações.
- **User Secrets**: armazenamento de credenciais durante o desenvolvimento.

## 📋 Requisitos

- .NET SDK compatível com a versão do projeto.
- Conta do Telegram.
- Credenciais de API obtidas em [my.telegram.org](https://my.telegram.org).

## ⚙️ Configuração

Configure as seguintes credenciais:

| Chave | Descrição |
|---|---|
| `Telegram:ApiId` | Identificador da aplicação Telegram. |
| `Telegram:ApiHash` | Hash da aplicação Telegram. |
| `Telegram:PhoneNumber` | Número de telefone da conta. |

Configure os User Secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "Telegram:ApiId" "SEU_API_ID"
dotnet user-secrets set "Telegram:ApiHash" "SEU_API_HASH"
dotnet user-secrets set "Telegram:PhoneNumber" "+5500000000000"
```

## ▶️ Execução

**1. Clone o repositório:**

```bash
git clone <URL_DO_REPOSITORIO>
cd TCM
```

**2. Compile o projeto:**

```bash
dotnet build -c Release
```

**3. Execute a aplicação:**

```bash
dotnet run --project TCM.CLI
```

## 📦 Publicação

Exemplo de publicação para Windows x64:

```powershell
dotnet publish .\TCM.CLI\TCM.CLI.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o .\publish
```

## ⚠️ Considerações

A transferência de participantes depende das permissões da conta, das configurações dos grupos e das restrições impostas pelo Telegram.

O TCM não garante que todos os participantes possam ser adicionados diretamente. Quando a plataforma impedir a operação, será necessário utilizar métodos de entrada autorizados, como links de convite.

## 📄 Licença

A licença do projeto deverá ser definida antes de sua distribuição.