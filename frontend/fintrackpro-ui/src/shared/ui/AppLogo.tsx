import { useState } from 'react'

type LogoSize = 'sm' | 'md' | 'lg'

interface AppLogoProps {
  size?: LogoSize
  showWordmark?: boolean
  className?: string
}

const sizePx: Record<LogoSize, number> = {
  sm: 28,
  md: 36,
  lg: 48,
}

const wordmarkSize: Record<LogoSize, string> = {
  sm: 'text-base',
  md: 'text-lg',
  lg: 'text-2xl',
}

export function AppLogo({ size = 'md', showWordmark = true, className }: AppLogoProps) {
  const [imgFailed, setImgFailed] = useState(false)
  const px = sizePx[size]

  return (
    <span className={`inline-flex items-center gap-2 ${className ?? ''}`}>
      {!imgFailed ? (
        <img
          src="/icon-192.png"
          alt="FinTrackPro"
          width={px}
          height={px}
          onError={() => setImgFailed(true)}
          style={{ width: px, height: px, objectFit: 'contain', borderRadius: '20%' }}
        />
      ) : (
        <img
          src="/favicon.svg"
          alt="FinTrackPro"
          width={px}
          height={px}
          style={{ width: px, height: px, objectFit: 'contain', borderRadius: '20%' }}
        />
      )}
      {showWordmark && (
        <span className={`font-bold tracking-tight ${wordmarkSize[size]}`}>
          FinTrackPro
        </span>
      )}
    </span>
  )
}
