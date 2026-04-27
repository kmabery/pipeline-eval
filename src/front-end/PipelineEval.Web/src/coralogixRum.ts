import { CoralogixRum, type CoralogixDomain } from '@coralogix/browser';

/**
 * Initializes Coralogix RUM when VITE_CORALOGIX_RUM_PUBLIC_KEY and VITE_CORALOGIX_DOMAIN are set.
 * Create a RUM API key in the Coralogix UI (RUM integration package); domain matches your account (e.g. US1, EU1).
 */
export function initCoralogixRum(): void {
  const publicKey = import.meta.env.VITE_CORALOGIX_RUM_PUBLIC_KEY?.trim();
  const domainRaw = import.meta.env.VITE_CORALOGIX_DOMAIN?.trim();
  if (!publicKey || !domainRaw) return;

  CoralogixRum.init({
    stringifyCustomLogData: true,
    public_key: publicKey,
    application: 'PipelineEval',
    version: import.meta.env.VITE_APP_VERSION ?? '0.0.0',
    coralogixDomain: domainRaw as CoralogixDomain,
    traceParentInHeader: {
      enabled: true,
    },
  });
}
