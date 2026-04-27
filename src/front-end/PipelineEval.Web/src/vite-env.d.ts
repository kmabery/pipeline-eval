/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_CORALOGIX_RUM_PUBLIC_KEY?: string;
  readonly VITE_CORALOGIX_DOMAIN?: string;
  readonly VITE_APP_VERSION?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
