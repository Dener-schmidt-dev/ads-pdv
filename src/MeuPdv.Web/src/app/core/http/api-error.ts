import { HttpErrorResponse } from '@angular/common/http';

export function extrairErroApi(err: unknown, fallback = 'Erro inesperado.'): string {
  if (!(err instanceof HttpErrorResponse)) {
    return fallback;
  }

  const body = err.error;
  if (typeof body === 'string' && body.trim()) {
    return body;
  }

  if (body && typeof body === 'object') {
    const payload = body as Record<string, unknown>;
    if (typeof payload['erro'] === 'string') return payload['erro'];
    if (typeof payload['title'] === 'string') return payload['title'];

    const errors = payload['errors'];
    if (errors && typeof errors === 'object') {
      const msgs = Object.values(errors as Record<string, unknown>)
        .flatMap((v) => (Array.isArray(v) ? v : [v]))
        .filter((v): v is string => typeof v === 'string');
      if (msgs.length) return msgs.join(' ');
    }
  }

  if (err.status === 404) {
    return 'Recurso não encontrado na API. Reinicie o servidor (dev.ps1) após atualizar o sistema.';
  }

  if (err.status === 0) {
    return 'Não foi possível conectar à API. Verifique se o servidor está em execução.';
  }

  return fallback;
}
