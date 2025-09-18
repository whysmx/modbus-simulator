"use client"

interface SearchHighlightProps {
  text: string
  searchTerm: string
  className?: string
}

export function SearchHighlight({ text, searchTerm, className = "" }: SearchHighlightProps) {
  if (!searchTerm.trim()) {
    return <span className={className}>{text}</span>
  }

  const regex = new RegExp(`(${searchTerm.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi')
  const parts = text.split(regex)

  return (
    <span className={className}>
      {parts.map((part, index) => {
        const isMatch = regex.test(part)
        return isMatch ? (
          <mark key={index} className="bg-yellow-200 text-yellow-900 rounded-sm px-0.5">
            {part}
          </mark>
        ) : (
          <span key={index}>{part}</span>
        )
      })}
    </span>
  )
}