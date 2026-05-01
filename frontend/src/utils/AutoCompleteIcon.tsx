"use client";

import { useMemo, useState } from "react";
import * as Icons from "lucide-react";

type Props = {
  value: string;
  onChange: (val: string) => void;
  placeholder?: string;
};

export function IconAutocomplete({ value, onChange, placeholder }: Props) {
  const [open, setOpen] = useState(false);

  const iconEntries = useMemo(() => Object.entries(Icons), []);

  const filtered = useMemo(() => {
    if (!value) return iconEntries.slice(0, 20);

    return iconEntries
      .filter(([name]) => name.toLowerCase().includes(value.toLowerCase()))
      .slice(0, 20);
  }, [value, iconEntries]);

  const SelectedIcon = Icons[
    value as keyof typeof Icons
  ] as React.ComponentType<any>;

  return (
    <div className="relative w-full">
      {/* INPUT + PREVIEW */}
      <div className="flex items-center gap-2">
        <input
          value={value}
          onChange={(e) => {
            onChange(e.target.value);
            setOpen(true);
          }}
          onFocus={() => setOpen(true)}
          placeholder={placeholder}
          className="w-full rounded-xl border border-[#e2e8f0] px-3 py-2 text-sm text-[#0f172a] focus:outline-none focus:ring-2 focus:ring-[#1a1f3c]/20"
        />

        <div className="flex h-10 w-10 items-center justify-center rounded-xl border border-[#e2e8f0] bg-[#f8fafc]">
          {SelectedIcon ? (
            <SelectedIcon size={18} className="text-[#1a1f3c]" />
          ) : (
            <span className="text-xs text-[#94a3b8]">?</span>
          )}
        </div>
      </div>

      {/* DROPDOWN */}
      {open && (
        <div className="absolute z-10 mt-2 max-h-60 w-full overflow-auto rounded-xl border border-[#e2e8f0] bg-white shadow-sm">
          {filtered.length === 0 && (
            <div className="p-3 text-sm text-[#94a3b8]">No icons found</div>
          )}

          {filtered.map(([name, Icon]) => {
            const Comp = Icon as React.ComponentType<any>;

            return (
              <button
                key={name}
                onClick={() => {
                  onChange(name);
                  setOpen(false);
                }}
                className="flex w-full items-center gap-3 px-3 py-2 text-left text-sm hover:bg-[#f8fafc]"
              >
                <Comp size={16} className="text-[#1a1f3c]" />
                <span className="text-[#0f172a]">{name}</span>
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
