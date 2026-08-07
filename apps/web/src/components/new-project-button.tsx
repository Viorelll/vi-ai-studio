import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { useCreateSpecification } from "@/hooks/use-specifications";

export function NewProjectButton() {
  const navigate = useNavigate();
  const createSpecification = useCreateSpecification();

  function handleClick() {
    createSpecification.mutate(
      { name: "Untitled Project", summary: "", owner: "You" },
      { onSuccess: (spec) => navigate(`/studio/${spec.id}`) },
    );
  }

  return (
    <Button onClick={handleClick} disabled={createSpecification.isPending} className="gap-2">
      <span className="text-base leading-none">+</span> {createSpecification.isPending ? "Creating…" : "New Project"}
    </Button>
  );
}
