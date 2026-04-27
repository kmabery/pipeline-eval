type BannerTone = 'error' | 'info'

type ErrorBannerProps = {
  message: string
  tone?: BannerTone
}

export function ErrorBanner({ message, tone = 'error' }: ErrorBannerProps) {
  const className = `banner ${tone === 'info' ? 'banner-info' : 'banner-error'}`
  return (
    <div className={className} role={tone === 'info' ? 'status' : 'alert'}>
      {message}
    </div>
  )
}
