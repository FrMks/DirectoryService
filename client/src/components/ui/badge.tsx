import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium tracking-tight transition-colors",
  {
    variants: {
      variant: {
        default:
          "border-zinc-200 bg-zinc-100 text-zinc-800 hover:bg-zinc-200",
        success:
          "border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100",
        muted:
          "border-amber-200 bg-amber-50 text-amber-700 hover:bg-amber-100",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
);

function Badge({
  className,
  variant,
  ...props
}: React.ComponentProps<"span"> & VariantProps<typeof badgeVariants>) {
  return (
    <span className={cn(badgeVariants({ variant }), className)} {...props} />
  );
}

export { Badge, badgeVariants };
