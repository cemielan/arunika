"use client";

import { useEffect, useRef, useState } from "react";

/** Spring-physics animation for a vector of numbers (e.g. [left, width]).
 * Steps a damped harmonic oscillator on every animation frame until it
 * settles near the target — gives continuous, fluid motion instead of the
 * fixed-duration feel of a CSS transition. */
export function useSpringVector(
  targets: number[],
  { stiffness = 280, damping = 24, mass = 1 } = {},
) {
  const [state, setState] = useState(() => ({
    values: targets,
    velocities: targets.map(() => 0),
  }));

  // Mutated only inside the rAF loop (never read/written during render).
  const valuesRef = useRef(targets);
  const velocitiesRef = useRef(targets.map(() => 0));
  const rafRef = useRef<number | undefined>(undefined);

  useEffect(() => {
    let lastTime = performance.now();

    function step(now: number) {
      const dt = Math.min((now - lastTime) / 1000, 0.064);
      lastTime = now;

      let settled = true;
      const nextValues = valuesRef.current.map((v, i) => {
        const t = targets[i];
        const springForce = -stiffness * (v - t);
        const dampingForce = -damping * velocitiesRef.current[i];
        velocitiesRef.current[i] += ((springForce + dampingForce) / mass) * dt;
        const nextV = v + velocitiesRef.current[i] * dt;

        if (Math.abs(t - nextV) > 0.1 || Math.abs(velocitiesRef.current[i]) > 0.1) {
          settled = false;
        }
        return nextV;
      });

      valuesRef.current = nextValues;
      setState({ values: [...nextValues], velocities: [...velocitiesRef.current] });

      if (!settled) {
        rafRef.current = requestAnimationFrame(step);
      }
    }

    rafRef.current = requestAnimationFrame(step);
    return () => {
      if (rafRef.current !== undefined) cancelAnimationFrame(rafRef.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [targets.join(","), stiffness, damping, mass]);

  return state;
}