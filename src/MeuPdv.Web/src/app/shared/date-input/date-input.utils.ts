const MESES = [
  'janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho',
  'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'
];

const DIAS_SEMANA = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'];

export interface DiaCalendario {
  data: string;
  dia: number;
  mesAtual: boolean;
  hoje: boolean;
  selecionado: boolean;
}

export function formatarDataBr(iso: string): string {
  if (!iso) return '';
  const [ano, mes, dia] = iso.split('-');
  if (!ano || !mes || !dia) return '';
  return `${dia}/${mes}/${ano}`;
}

function paraIsoLocal(data: Date): string {
  const ano = data.getFullYear();
  const mes = String(data.getMonth() + 1).padStart(2, '0');
  const dia = String(data.getDate()).padStart(2, '0');
  return `${ano}-${mes}-${dia}`;
}

export function hojeIso(): string {
  return paraIsoLocal(new Date());
}

export function labelMesAno(ano: number, mes: number): string {
  return `${MESES[mes]} de ${ano}`;
}

export function diasSemana(): string[] {
  return DIAS_SEMANA;
}

export function montarGradeCalendario(ano: number, mes: number, selecionado?: string): DiaCalendario[] {
  const primeiro = new Date(ano, mes, 1);
  const inicioGrid = new Date(ano, mes, 1 - primeiro.getDay());
  const hoje = hojeIso();
  const dias: DiaCalendario[] = [];

  for (let i = 0; i < 42; i++) {
    const atual = new Date(inicioGrid);
    atual.setDate(inicioGrid.getDate() + i);
    const iso = paraIsoLocal(atual);
    dias.push({
      data: iso,
      dia: atual.getDate(),
      mesAtual: atual.getMonth() === mes,
      hoje: iso === hoje,
      selecionado: iso === selecionado
    });
  }

  return dias;
}
