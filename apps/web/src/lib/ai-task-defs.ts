import type { AiProvider, AiTaskType } from "@/lib/types";

export const TASK_DEFS: { key: AiTaskType; label: string; desc: string }[] = [
  { key: "specGeneration", label: "Spec generation", desc: "Drafts markdown for a wizard phase in AI Specification Studio." },
  { key: "codeGeneration", label: "Code generation", desc: "Powers AI Build code generation." },
  { key: "imageGeneration", label: "Image generation", desc: "Generates product & UI imagery." },
  { key: "soundGeneration", label: "Sound generation", desc: "Generates audio & music assets." },
  { key: "transcribing", label: "Transcribing", desc: "Speech-to-text for voice input." },
];

export const PROVIDERS: { value: AiProvider; label: string }[] = [
  { value: "openAi", label: "OpenAI" },
  { value: "anthropic", label: "Anthropic" },
  { value: "google", label: "Google" },
  { value: "mistral", label: "Mistral" },
  { value: "elevenLabs", label: "ElevenLabs" },
  { value: "azureOpenAi", label: "Azure OpenAI" },
  { value: "custom", label: "Custom" },
];
