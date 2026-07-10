"use client";

import { Button, useTheme } from "@heroui/react";
import { motion } from "framer-motion";
import { Moon, Sun } from "lucide-react";

export function ThemeSwitch() {
  const { resolvedTheme, setTheme } = useTheme();
  const isDark = resolvedTheme === "dark";

  return (
    <Button
      isIconOnly
      radius="full"
      size="sm"
      variant="flat"
      aria-label="Toggle theme"
      onPress={() => setTheme(isDark ? "light" : "dark")}
    >
      <motion.span
        initial={false}
        animate={{ scale: isDark ? 1.02 : 1 }}
        transition={{ duration: 0.14, ease: "easeOut" }}
        className="relative inline-flex h-4 w-4 items-center justify-center will-change-transform"
      >
        <Sun className={`absolute size-3.5 transition-opacity duration-100 ${isDark ? "opacity-0" : "opacity-100"}`} />
        <Moon className={`absolute size-3.5 transition-opacity duration-100 ${isDark ? "opacity-100" : "opacity-0"}`} />
      </motion.span>
    </Button>
  );
}
