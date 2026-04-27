import { CognitoUserPool } from 'amazon-cognito-identity-js'

const poolId = import.meta.env.VITE_COGNITO_USER_POOL_ID as string | undefined
const clientId = import.meta.env.VITE_COGNITO_CLIENT_ID as string | undefined

export const cognitoConfigured = Boolean(poolId && clientId)

export const userPool = cognitoConfigured
  ? new CognitoUserPool({
      UserPoolId: poolId!,
      ClientId: clientId!,
    })
  : null
