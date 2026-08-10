import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { useCreateSpecification } from "@/hooks/use-specifications";

export function NewProjectButton() {
  const navigate = useNavigate();
  const createSpecification = useCreateSpecification();

  function handleClick() {
    createSpecification.mutate(
      { name: "", summary: "", owner: "You" },
      { onSuccess: (spec) => navigate(`/studio/${spec.id}`) },
    );
  }

  return (
    <Button
      onClick={handleClick}
      disabled={createSpecification.isPending}
      className="h-9 rounded-lg px-4 text-[13px]"
    >
      <span className="text-base leading-none">+</span>{" "}
      {createSpecification.isPending ? "Creating…" : "New Project"}
    </Button>
  );
}
