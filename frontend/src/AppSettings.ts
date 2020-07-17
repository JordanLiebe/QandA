export const server = 'https://api.jmliebe.com/qanda';

export const webAPIUrl = `${server}/api`;

export const authSettings = {
  domain: 'jmliebe.us.auth0.com',
  client_id: 'Cn447snduB8btKdnBjgw6XPhmS9mwAY9',
  redirect_uri: window.location.origin + '/signin-callback',
  scope: 'openid profile QandAAPI email',
  audience: 'https://qanda',
};
