import { useState } from "react";
import { DownloadIcon, LoaderCircle } from "lucide-react";
import { buttonVariants } from "@/components/ui/button";
import { downloadFile } from "@/lib/api-client";
import { cn } from "@/lib/utils";

interface DownloadButtonProps {
  path: string;
  filename: string;
  size?: "sm" | "default";
  variant?: "outline" | "default";
}

export function DownloadButton({
  path,
  filename,
  size = "default",
  variant = "default",
}: DownloadButtonProps) {
  const [isDownloading, setIsDownloading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleDownload = async () => {
    setIsDownloading(true);
    setError(null);
    try {
      await downloadFile(path, filename);
    } catch (downloadError) {
      setError(
        downloadError instanceof Error
          ? downloadError.message
          : "Download failed.",
      );
    } finally {
      setIsDownloading(false);
    }
  };

  return (
    <span className="inline-flex flex-col items-end gap-1">
      <button
        type="button"
        disabled={isDownloading}
        onClick={() => void handleDownload()}
        className={cn(buttonVariants({ variant, size }))}
      >
        {isDownloading ? (
          <LoaderCircle className="animate-spin" />
        ) : (
          <DownloadIcon />
        )}
        {isDownloading ? "Preparing..." : "Download .zip"}
      </button>
      {error && (
        <span className="text-[11px] font-normal text-destructive">
          {error}
        </span>
      )}
    </span>
  );
}
