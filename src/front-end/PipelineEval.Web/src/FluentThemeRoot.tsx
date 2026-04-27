import { FluentProvider, webDarkTheme, webLightTheme } from '@fluentui/react-components'
import { useEffect, useState, type ReactNode } from 'react'

export function FluentThemeRoot({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState(webLightTheme)

  useEffect(() => {
    const mq = window.matchMedia('(prefers-color-scheme: dark)')
    const apply = () => setTheme(mq.matches ? webDarkTheme : webLightTheme)
    apply()
    mq.addEventListener('change', apply)
    return () => mq.removeEventListener('change', apply)
  }, [])

  return <FluentProvider theme={theme}>{children}</FluentProvider>
}
