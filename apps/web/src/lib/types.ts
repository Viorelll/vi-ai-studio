// Mirrors the response contracts in apps/api/src/ViAiStudio.Api/Contracts.
// Kept hand-written for now; consider generating this from the OpenAPI
// document (GET /openapi/v1.json) once the contract stabilizes.

export type SpecificationStatus = "draft" | "building" | "ready" | "failed";
export type GenerationStatus = "running" | "ready" | "failed";
export type AiProvider =
  | "openAi"
  | "anthropic"
  | "google"
  | "mistral"
  | "elevenLabs"
  | "azureOpenAi"
  | "custom";
export type AiTaskType =
  | "specGeneration"
  | "codeGeneration"
  | "imageGeneration"
  | "soundGeneration"
  | "transcribing";

export interface TechStack {
  backend: string;
  ui: string;
  database: string;
  infra: string;
  uiStyle: string;
}

export interface SpecificationSummary {
  id: string;
  name: string;
  summary: string;
  status: SpecificationStatus;
  owner: string;
  created: string;
  progress: number;
  stack: TechStack;
  generationCount: number;
  latestGenerationVersion: number | null;
  latestGenerationStatus: GenerationStatus | null;
}

export interface SpecificationPhase {
  phaseIndex: number;
  title: string;
  items: string[];
  output: string;
  checkedItems: string[];
  selectedKeywords: string[];
  generatedText: string | null;
}

export interface GenerationSummary {
  id: string;
  specificationId: string;
  version: number;
  status: GenerationStatus;
  note: string;
  model: string;
  stack: TechStack;
  created: string;
  durationSeconds: number | null;
}

export interface SpecificationDetail {
  id: string;
  name: string;
  summary: string;
  description: string;
  features: string;
  audience: string;
  status: SpecificationStatus;
  owner: string;
  created: string;
  progress: number;
  stack: TechStack;
  specMarkdown: string | null;
  phases: SpecificationPhase[];
  generations: GenerationSummary[];
}

export interface SpecificationPhaseDefinition {
  index: number;
  title: string;
  items: string[];
  output: string;
}

export interface AiModelConfig {
  id: string;
  label: string;
  provider: AiProvider;
  model: string;
  baseUrl: string;
  maskedApiKey: string;
}

export interface TaskRouting {
  task: AiTaskType;
  aiModelConfigId: string | null;
}

export interface GenerationDetail {
  id: string;
  specificationId: string;
  version: number;
  status: GenerationStatus;
  note: string;
  model: string;
  stack: TechStack;
  created: string;
  durationSeconds: number | null;
  fileTree: string[];
}

export interface AiCallLogSummary {
  id: string;
  specificationId: string;
  generationVersion: number | null;
  task: AiTaskType;
  model: string;
  requests: number;
  tokensIn: number;
  tokensOut: number;
  created: string;
}

export interface AiCallLogDetail {
  id: string;
  task: AiTaskType;
  model: string;
  requests: number;
  tokensIn: number;
  tokensOut: number;
  prompt: string;
  result: string;
  created: string;
}

export interface AiCallLogRollup {
  specificationId: string;
  logCount: number;
  totalRequests: number;
  totalTokens: number;
}

export interface BuildEvent {
  generationId: string;
  stepLabel: string;
  logLine: string;
  progressPct: number;
  done: boolean;
}
