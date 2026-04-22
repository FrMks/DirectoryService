"use client";

import { JSX, useState } from "react";
import { Badge } from "../ui/badge";
import { Button } from "../ui/button";
import { Checkbox } from "../ui/checkbox";
import { Input } from "../ui/input";

type Todo = {
  id: number;
  text: string;
  completed: boolean;
};

export default function Todo(): JSX.Element {
  const [todos, setTodos] = useState<Todo[]>([
    { id: 1, text: "Learn React", completed: false },
    { id: 2, text: "Build a Todo App", completed: true },
    { id: 3, text: "Master TypeScript", completed: false },
  ]);

  const [input, setInput] = useState("");

  const addTodo = () => {
    const newTodo: Todo = {
      id: todos.length + 1,
      text: input,
      completed: false,
    };

    setTodos((prevTodos) => [...prevTodos, newTodo]);
  };

  const toggleTodo = (id: number) => {
    setTodos((prevTodos) =>
      prevTodos.map((todo) => {
        if (todo.id === id) {
          return { ...todo, completed: !todo.completed };
        }
        return todo;
      }),
    );
  };

  return (
    <div className="space-y-4">
      <div className="flex gap-2">
        <Input
          placeholder="Добавить задачу"
          className="flex-1"
          value={input}
          onChange={(event) => setInput(event.target.value)}
        />
        <Button onClick={addTodo}>Добавить задачу</Button>
      </div>

      {todos.map((todo) => (
        <div
          key={todo.id}
          className="flex items-center justify-between gap-4 rounded-xl border border-zinc-200 bg-white px-4 py-3 shadow-sm"
        >
          <div className="flex min-w-0 items-center gap-3">
            <Badge
              variant="default"
              className="min-w-8 justify-center rounded-md"
            >
              {todo.id}
            </Badge>
            <Badge
              variant="default"
              className="max-w-full rounded-md bg-zinc-50 text-zinc-800"
            >
              <span className="truncate">{todo.text}</span>
            </Badge>
          </div>
          <div className="flex items-center gap-3">
            <Checkbox
              checked={todo.completed}
              onCheckedChange={() => toggleTodo(todo.id)}
              aria-label="Toggle task completion"
              className="size-6 rounded-lg"
            />
            <Badge
              variant={todo.completed ? "success" : "muted"}
              className="rounded-md"
            >
              {todo.completed ? "Completed" : "Not Completed"}
            </Badge>
          </div>
        </div>
      ))}
    </div>
  );
}
