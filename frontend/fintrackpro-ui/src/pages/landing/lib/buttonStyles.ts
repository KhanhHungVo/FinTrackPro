import { useState } from 'react'
import type { CSSProperties } from 'react'

const SPRING = 'transform 0.22s cubic-bezier(0.34, 1.56, 0.64, 1)'
const EASE_PROPS = 'box-shadow 0.2s ease-out, background 0.18s ease-out, color 0.18s ease-out, border-color 0.18s ease-out'
export const TRANSITION = `${SPRING}, ${EASE_PROPS}`

const SHADOW_PRIMARY_BASE = '0 1px 2px rgba(0,0,0,0.5), 0 4px 12px rgba(37,99,235,0.30), 0 8px 32px rgba(37,99,235,0.18)'
const SHADOW_PRIMARY_HOVER = '0 2px 4px rgba(0,0,0,0.4), 0 8px 24px rgba(37,99,235,0.50), 0 16px 48px rgba(37,99,235,0.28), 0 0 0 1px rgba(59,130,246,0.25)'
const SHADOW_PRIMARY_PRESS = '0 1px 2px rgba(0,0,0,0.6), 0 2px 8px rgba(37,99,235,0.25), 0 4px 16px rgba(37,99,235,0.15)'

const SHADOW_OUTLINE_BASE = '0 1px 2px rgba(0,0,0,0.4), 0 4px 16px rgba(0,0,0,0.2)'
const SHADOW_OUTLINE_HOVER = '0 2px 4px rgba(0,0,0,0.3), 0 8px 24px rgba(37,99,235,0.20), 0 0 0 1px rgba(255,255,255,0.20)'
const SHADOW_OUTLINE_PRESS = '0 1px 2px rgba(0,0,0,0.5), 0 2px 8px rgba(0,0,0,0.15)'

export function useButtonState() {
  const [isHovered, setHovered] = useState(false)
  const [isPressed, setPressed] = useState(false)
  return {
    isHovered,
    isPressed,
    handlers: {
      onMouseEnter: () => setHovered(true),
      onMouseLeave: () => { setHovered(false); setPressed(false) },
      onMouseDown: () => setPressed(true),
      onMouseUp: () => setPressed(false),
    },
  }
}

export function primaryBtnStyle(
  isHovered: boolean,
  isPressed: boolean,
  overrides?: CSSProperties,
): CSSProperties {
  return {
    border: 'none',
    cursor: 'pointer',
    color: '#fff',
    fontWeight: 700,
    borderRadius: 10,
    transition: TRANSITION,
    background: isHovered
      ? 'linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)'
      : '#2563eb',
    transform: isPressed
      ? 'translateY(0px) scale(0.97)'
      : isHovered
        ? 'translateY(-2px)'
        : 'translateY(0px)',
    boxShadow: isPressed
      ? SHADOW_PRIMARY_PRESS
      : isHovered
        ? SHADOW_PRIMARY_HOVER
        : SHADOW_PRIMARY_BASE,
    ...overrides,
  }
}

export function outlineBtnStyle(
  isHovered: boolean,
  isPressed: boolean,
  overrides?: CSSProperties,
): CSSProperties {
  return {
    cursor: 'pointer',
    borderRadius: 9,
    transition: TRANSITION,
    border: isHovered
      ? '1px solid rgba(255,255,255,0.30)'
      : '1px solid rgba(255,255,255,0.12)',
    background: isHovered ? 'rgba(255,255,255,0.07)' : 'transparent',
    color: isHovered ? 'rgba(255,255,255,0.92)' : 'rgba(255,255,255,0.55)',
    transform: isPressed
      ? 'translateY(0px) scale(0.97)'
      : isHovered
        ? 'translateY(-2px)'
        : 'translateY(0px)',
    boxShadow: isPressed
      ? SHADOW_OUTLINE_PRESS
      : isHovered
        ? SHADOW_OUTLINE_HOVER
        : SHADOW_OUTLINE_BASE,
    ...overrides,
  }
}

export function ghostLinkStyle(isHovered: boolean, isPressed: boolean): CSSProperties {
  return {
    display: 'inline-block',
    padding: '14px 28px',
    borderRadius: 10,
    border: isHovered
      ? '1px solid rgba(255,255,255,0.20)'
      : '1px solid rgba(255,255,255,0.12)',
    color: isHovered ? 'rgba(255,255,255,0.85)' : 'rgba(255,255,255,0.55)',
    fontSize: 16,
    fontWeight: 600,
    textDecoration: 'none',
    transition: TRANSITION,
    transform: isPressed
      ? 'translateY(0px) scale(0.97)'
      : isHovered
        ? 'translateY(-2px)'
        : 'translateY(0px)',
    boxShadow: isPressed
      ? '0 1px 2px rgba(0,0,0,0.5)'
      : isHovered
        ? '0 2px 4px rgba(0,0,0,0.3), 0 8px 20px rgba(0,0,0,0.25)'
        : 'none',
  }
}

export function hamburgerBtnStyle(
  isHovered: boolean,
  isPressed: boolean,
  drawerOpen: boolean,
): CSSProperties {
  return {
    width: 36,
    height: 36,
    borderRadius: 8,
    border: '1px solid rgba(255,255,255,0.12)',
    background: isPressed
      ? 'rgba(255,255,255,0.12)'
      : isHovered || drawerOpen
        ? 'rgba(255,255,255,0.07)'
        : 'transparent',
    color: isHovered ? 'rgba(255,255,255,0.92)' : 'rgba(255,255,255,0.7)',
    fontSize: 18,
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
    transition: 'background 0.15s ease-out, color 0.15s ease-out, transform 0.12s ease-out',
    transform: isPressed ? 'scale(0.93)' : 'scale(1)',
  }
}
