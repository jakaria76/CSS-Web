# UI & Design System Rules

Always build and style every UI page, view, form, modal, table, and component in **shadcn/ui**:

- **Design Language & Tokens:** Use shadcn/ui Zinc neutrals (`#ffffff` background, `#fafafa` subtle surface, `#09090b` foreground, `#f4f4f5` muted, `#71717a` muted-foreground, `#e4e4e7` 1px hairline borders) with the project's **Emerald Green** (`#15803d`, hover `#166534`, soft `#f0fdf4`) as the primary accent.
- **Typography & Geometry:** Use `Inter` and `Noto Sans Bengali`, `0.5rem` (`8px`) standard radius (`0.75rem` cards), minimal `xs`/`sm` shadows, `1px solid` borders, and shadcn focus rings (`0 0 0 2px #ffffff, 0 0 0 4px var(--ring)`).
- **Components & Patterns:** Use light `#fafafa` page headers with subtle dot-grids (avoid loud gradients), segmented `TabsList` filter controls, hairline bordered Cards and Tables (`thead` in `#fafafa`), and consistent `.btn-css-primary` / `.btn-css-outline` controls defined in `wwwroot/css/site.css`.
- **Accessibility & Contrast:** Never use light/white button classes (`btn-light`, `btn-outline-light`, `text-light`) on light `#ffffff`/`#fafafa` backgrounds.
