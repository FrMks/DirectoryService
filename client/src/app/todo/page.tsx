import Todo from "@/components/todo/todo";
import { JSX } from "react";

export default function TodoPage(): JSX.Element {
  return (
    <main className="p-10">
      <Todo />
    </main>
  );
}
