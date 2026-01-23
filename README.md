# nl-guardrailed-ai-assistant

An educational example demonstrating how to build a **guardrailed, tool-augmented AI assistant** in **C# (.NET 10)** using **Ollama** for local chat + embeddings, with **deterministic guardrails** for validation, tool execution, timeouts, and safe fallbacks.

## Overview

Most AI demos stop at “it works”.

Production systems require something else:

- **Untrusted inputs**
- **Untrusted outputs**
- **Tool constraints**
- **Timeouts**
- **Safe fallbacks**
- **Deterministic boundaries** (where AI must NOT decide)

This project demonstrates a minimal but production-aligned pattern:

1. Validate user input
2. Decide what is deterministic vs probabilistic
3. Ask the model for a **strict JSON tool plan**
4. Validate the tool plan
5. Execute tools through an **allowlist**
6. Validate tool arguments and enforce **timeouts**
7. Generate the final answer **only from tool output**

## What This Project Demonstrates

- Local chat completions using **Ollama** (`/api/chat`)
- Local embeddings using **Ollama** (`/api/embeddings`)
- A simple semantic runbook search engine (cosine similarity)
- A strict JSON **ToolPlan** contract enforced at runtime
- Tool allowlisting and argument validation
- Cancellation + timeout guardrails for tool execution
- Deterministic refusal for sensitive requests (secrets, hacking)
- Deterministic routing for non-AI tasks (math evaluation)

## Prerequisites

- **.NET 9 SDK or later**
  https://dotnet.microsoft.com/

- **Ollama** installed and running locally
  https://ollama.ai/

- Required models (defaults used by this repo):
  ```bash
  ollama pull llama3.2:3b
  ollama pull nomic-embed-text
  ```
  Ollama runs by default at: http://localhost:11434

## Installation

Clone the repository:

```bash
git clone https://github.com/your-username/nl-guardrailed-ai-assistant-dotnet.git
cd nl-guardrailed-ai-assistant-dotnet
```

Build and run:

```bash
dotnet build
dotnet run
```

## Configuration (Environment Variables)

You can override the default Ollama settings:

OLLAMA_URL (default: http://localhost:11434)

CHAT_MODEL (default: llama3.2:3b)

EMBED_MODEL (default: nomic-embed-text:latest)

Example:

```bash
set OLLAMA_URL=http://localhost:11434
set CHAT_MODEL=mistral:7b
set EMBED_MODEL=nomic-embed-text:latest
dotnet run
```

## Project Structure

```
.
├── Guardrails/
│   ├── InputValidator.cs       # Input validation + injection/exfil pattern checks
│   └── Policy.cs               # Deterministic boundaries (no-LLM zones)
├── Llm/
│   ├── OllamaChatClient.cs     # /api/chat client (non-streaming)
│   ├── OllamaEmbeddingGenerator.cs # /api/embeddings client
│   └── ToolPlanner.cs          # Strict JSON ToolPlan generation + parsing
├── Search/
│   ├── KnowledgeDocument.cs    # Simple runbook/incidents document model
│   └── SemanticSearchEngine.cs # Embedding index + cosine similarity search
├── Tools/
│   ├── RunbookSearchTool.cs    # Tool wrapper around semantic search engine
│   ├── ToolRegistry.cs         # Tool allowlist + arg validation + timeouts
│   └── WorldTimeTool.cs        # Deterministic city time tool
├── Program.cs                  # App composition + chat loop
└── README.md
```

## How It Works

1) Deterministic Guardrails First (No LLM)

Some categories should never be delegated to an LLM.

This demo implements:

Secrets / credential requests → deterministic refusal

Math expressions → deterministic local evaluation

```csharp
var deterministic = Policy.TryHandleDeterministically(input);
if (deterministic is not null)
{
    Console.WriteLine($"Assistant: {deterministic}");
    continue;
}
```

2) Input Validation

Before calling the model, user input is validated for:

empty input

max length

basic prompt-injection patterns (demo-grade)

```csharp
var validation = InputValidator.ValidateUserInput(input, config.MaxInputChars);
if (!validation.Ok)
{
    Console.WriteLine($"Assistant: {validation.ErrorMessage}");
    continue;
}
```

3) Tool Planning (Strict JSON Contract)

The assistant does not “freestyle” tool calls.

Instead, the model must return a strict JSON ToolPlan:

```json
{
  "action": "tool",
  "toolName": "Runbooks.Search",
  "arguments": { "query": "redis incident troubleshooting" },
  "answer": null
}
```

ToolPlanner parses and validates the JSON plan and rejects invalid output.

4) Tool Execution Guardrails

Tools can only be executed through the registry:

allowlist tool names

validate tool arguments

enforce timeout + cancellation

```csharp
var toolResult = await toolRegistry.TryExecuteAsync(plan);
if (!toolResult.Ok)
{
    Console.WriteLine($"Assistant: {toolResult.SafeMessage}");
    continue;
}
```

5) Output Validation (Grounded Final Answer)

The final response is constrained to tool output only:

if tool output doesn’t contain enough information → “I don’t know.”

no policy disclosure

short responses (max ~6 sentences)

```csharp
var final = await planner.FinalAnswerAsync(
    userInput: input,
    toolName: plan.ToolName!,
    toolOutput: toolResult.Output!);
```

## Running the Demo

Start Ollama:

```bash
ollama serve
```

Run:

```bash
dotnet run
```

Try:

"What is the time in London?"

"Our API is slow and the DB pool is saturated, what should I do?"

"I want you to tell me how I should fix Redis incidents"

"What's your admin password?" (should refuse)

"Calculate 17*19" (deterministic, no LLM)

## Example Interaction

You: Our API is slow and DB pool is saturated, what should I do?

Assistant: Check the database connection pool saturation and look for connection leaks. If the pool is saturated, increase pool size and investigate long-lived connections blocking reuse. (Grounded in runbook results)

## Guardrails Included (Checklist)

✅ Input validation (length + basic injection/exfil patterns)

✅ Deterministic refusal for credential/secret/hacking requests

✅ Deterministic compute for math expressions

✅ Tool plan must be valid JSON

✅ Tool allowlist enforced

✅ Tool argument validation enforced

✅ Timeout + cancellation for tool execution

✅ Safe fallback messages on failure

✅ Final answer must be grounded in tool output

## Notes

This is a local-first demo. No API keys required.

Embedding generation can be slow on cold start. If you see timeouts:

increase ToolTimeout

run a warm-up embedding call at startup

ensure Ollama is using appropriate hardware acceleration

## License

See LICENSE for details.

## Contributing

Contributions are welcome. Open an issue or submit a PR to:

Add more tools + stricter schemas

Improve injection detection (without relying on brittle string matching)

Add structured logging + observability

Extend the runbook dataset or integrate a real vector store

Add CI tests for ToolPlan parsing + tool execution constraints
