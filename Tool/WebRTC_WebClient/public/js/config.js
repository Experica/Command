import {getServers} from "./icesettings.js";

// --- Default wsUrl handling ---
const params = new URLSearchParams(window.location.search);
if (!params.has('wsUrl')) {
  params.set('wsUrl', 'ws://localhost:3000');
  // Update URL without reloading
  window.history.replaceState({}, '', window.location.pathname + '?' + params.toString());
}

export const signalingUrl = params.get('wsUrl');


export async function getServerConfig() {
  // Extract protocol + host from the signaling URL
  const signalingHttpBase = signalingUrl.replace(/^ws/, 'http').replace(/\/$/, '');
  const protocolEndPoint = `${signalingHttpBase}/config`;
  const createResponse = await fetch(protocolEndPoint);
  return await createResponse.json();
}

export function getRTCConfiguration() {
  let config = {};
  config.sdpSemantics = 'unified-plan';
  config.iceServers = getServers();
  return config;
}
