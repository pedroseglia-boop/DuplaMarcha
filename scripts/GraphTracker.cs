// GraphTracker.cs – versão Unity (Compatível com Unity 6)
// Requer: EPPlus 8.6.1 DLL em Assets/Plugins/EPPlus.dll
// Como usar: chame GraphTracker.GerarExcel(csvPath, xlsxPath) a partir de qualquer MonoBehaviour

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;

public static class GraphTracker
{
    // ─────────────────────────────────────────────
    // Cores em hex
    // ─────────────────────────────────────────────
    private const string COR_AZUL_ESCURO = "1F497D"; // cabeçalhos principais
    private const string COR_AZUL_CLARO = "BDD7EE";  // cabeçalho secundário
    private const string COR_ZEBRA = "D9E1F2";       // linhas alternadas
    private const string COR_BRANCO = "FFFFFF";      // texto sobre fundo escuro

    // ─────────────────────────────────────────────
    // Modelo de dados
    // ─────────────────────────────────────────────
    private class Registro
    {
        public double Tempo { get; set; }
        public string Id { get; set; }
        public double RotX { get; set; }
        public double RotY { get; set; }
        public double RotZ { get; set; }
        public double DistanciaPasso { get; set; }
        public double DistanciaTotal { get; set; }
    }

    // Mapeamento de número do tracker → nome amigável
    private static readonly Dictionary<int, string> NomesTrackers = new Dictionary<int, string>
    {
        { 1, "Quadril"       },
        { 2, "Coxa Esquerda" },
        { 3, "Coxa Direita"  },
        { 4, "Pe Esquerdo"   },
        { 5, "Pe Direito"    },
        { 6, "Tornozelo"     },
    };

    // ─────────────────────────────────────────────
    // Ponto de entrada público
    // ─────────────────────────────────────────────
    public static void GerarExcel(string csvPath, string xlsxPath)
    {
        ExcelPackage.License.SetNonCommercialPersonal("pedro");

        List<Registro> dados = LerCsv(csvPath);

        if (dados.Count == 0)
        {
            UnityEngine.Debug.LogWarning("[GraphTracker] Nenhum dado encontrado no CSV: " + csvPath);
            return;
        }

        using (var excel = new ExcelPackage())
        {
            CriarPlanilhaDados(excel, dados);
            CriarPlanilhaDistancia(excel, dados);
            CriarPlanilhaPassos(excel, dados);
            CriarPlanilhaRotacao(excel, dados, 1, "Quadril");
            CriarPlanilhaRotacao(excel, dados, 4, "Pe Esquerdo");
            CriarPlanilhaRotacao(excel, dados, 5, "Pe Direito");
            CriarPlanilhaComparacao(excel, dados);
            CriarDashboard(excel, dados);

            excel.SaveAs(new FileInfo(xlsxPath));
        }

        UnityEngine.Debug.Log("[GraphTracker] Excel gerado em: " + xlsxPath);
    }

    // ─────────────────────────────────────────────
    // Leitura do CSV
    // ─────────────────────────────────────────────
    private static List<Registro> LerCsv(string path)
    {
        var lista = new List<Registro>();

        if (!File.Exists(path))
        {
            UnityEngine.Debug.LogError("[GraphTracker] Arquivo nao encontrado: " + path);
            return lista;
        }

        string[] linhas = File.ReadAllLines(path);
        if (linhas.Length < 2) return lista;

        // Detecta índices pelo cabeçalho (case-insensitive)
        string[] cabecalho = linhas[0].Split(',');
        int idxId = FindIndex(cabecalho, "id");
        int idxTempo = FindIndex(cabecalho, "tempo");
        int idxRotX = FindIndex(cabecalho, "rotx");
        int idxRotY = FindIndex(cabecalho, "roty");
        int idxRotZ = FindIndex(cabecalho, "rotz");

        // Fallback para ordem padrão id,tempo,rotX,rotY,rotZ
        if (idxId < 0) idxId = 0;
        if (idxTempo < 0) idxTempo = 1;
        if (idxRotX < 0) idxRotX = 2;
        if (idxRotY < 0) idxRotY = 3;
        if (idxRotZ < 0) idxRotZ = 4;

        int colMax = Math.Max(idxId, Math.Max(idxTempo, Math.Max(idxRotX, Math.Max(idxRotY, idxRotZ))));

        var ultimasPosicoes = new Dictionary<string, double[]>();
        var distanciasAcum = new Dictionary<string, double>();

        for (int i = 1; i < linhas.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(linhas[i])) continue;

            string[] cols = linhas[i].Split(',');
            if (cols.Length <= colMax) continue;

            var reg = new Registro
            {
                Id = cols[idxId].Trim(),
                Tempo = ParseDouble(cols[idxTempo]),
                RotX = ParseDouble(cols[idxRotX]),
                RotY = ParseDouble(cols[idxRotY]),
                RotZ = ParseDouble(cols[idxRotZ]),
            };

            // Distância angular entre amostras consecutivas do mesmo tracker
            double passo = 0;
            if (ultimasPosicoes.ContainsKey(reg.Id))
            {
                double[] prev = ultimasPosicoes[reg.Id];
                double dx = reg.RotX - prev[0];
                double dy = reg.RotY - prev[1];
                double dz = reg.RotZ - prev[2];
                passo = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }

            ultimasPosicoes[reg.Id] = new double[] { reg.RotX, reg.RotY, reg.RotZ };

            if (!distanciasAcum.ContainsKey(reg.Id)) distanciasAcum[reg.Id] = 0;
            distanciasAcum[reg.Id] += passo;

            reg.DistanciaPasso = passo;
            reg.DistanciaTotal = distanciasAcum[reg.Id];

            lista.Add(reg);
        }

        return lista;
    }

    private static int FindIndex(string[] arr, string chave)
    {
        for (int i = 0; i < arr.Length; i++)
            if (arr[i].Trim().Equals(chave, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    private static double ParseDouble(string s)
    {
        double v;
        if (double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
        return 0;
    }

    // ─────────────────────────────────────────────
    // Helpers de estilo (Corrigidos para Unity)
    // ─────────────────────────────────────────────
    private static void EstilizarCabecalho(ExcelRange celulas)
    {
        celulas.Style.Font.Bold = true;
        celulas.Style.Fill.PatternType = ExcelFillStyle.Solid;

        celulas.Style.Fill.BackgroundColor.SetColor(HexParaColor(COR_AZUL_ESCURO));
        celulas.Style.Font.Color.SetColor(HexParaColor(COR_BRANCO));

        celulas.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    }

    private static void AplicarZebra(ExcelRange celulas)
    {
        celulas.Style.Fill.PatternType = ExcelFillStyle.Solid;
        celulas.Style.Fill.BackgroundColor.SetColor(HexParaColor(COR_ZEBRA));
    }

    private static System.Drawing.Color HexParaColor(string hex)
    {
        hex = hex.Replace("#", "");
        if (hex.Length == 6)
        {
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return System.Drawing.Color.FromArgb(r, g, b);
        }
        return System.Drawing.Color.White;
    }

    private static ExcelChart CriarGraficoBase(ExcelWorksheet ws, string nome, string titulo,
        eChartType tipo, int linhaPos, int colPos, int largura, int altura)
    {
        var chart = ws.Drawings.AddChart(nome, tipo);
        chart.Title.Text = titulo;
        chart.SetPosition(linhaPos, 0, colPos, 0);
        chart.SetSize(largura, altura);
        return chart;
    }

    private static string NomeAmigavel(string id)
    {
        foreach (var kv in NomesTrackers)
            if (id.Contains("/trackers/" + kv.Key + "/")) return kv.Value;
        return id;
    }

    // ─────────────────────────────────────────────
    // 1. Planilha DADOS
    // ─────────────────────────────────────────────
    private static void CriarPlanilhaDados(ExcelPackage excel, List<Registro> dados)
    {
        var ws = excel.Workbook.Worksheets.Add("Dados");

        string[] cols = { "Tempo (s)", "Tracker (ID)", "Rot X", "Rot Y", "Rot Z", "Dist. Passo", "Dist. Total" };
        for (int c = 0; c < cols.Length; c++)
            ws.Cells[1, c + 1].Value = cols[c];

        EstilizarCabecalho(ws.Cells[1, 1, 1, cols.Length]);

        int linha = 2;
        foreach (var d in dados)
        {
            ws.Cells[linha, 1].Value = d.Tempo;
            ws.Cells[linha, 2].Value = d.Id;
            ws.Cells[linha, 3].Value = d.RotX;
            ws.Cells[linha, 4].Value = d.RotY;
            ws.Cells[linha, 5].Value = d.RotZ;
            ws.Cells[linha, 6].Value = d.DistanciaPasso;
            ws.Cells[linha, 7].Value = d.DistanciaTotal;

            if (linha % 2 == 0)
                AplicarZebra(ws.Cells[linha, 1, linha, cols.Length]);

            linha++;
        }

        ws.Cells.AutoFitColumns();
        ws.View.FreezePanes(2, 1);
    }

    // ─────────────────────────────────────────────
    // 2. Planilha DISTÂNCIA TOTAL
    // ─────────────────────────────────────────────
    private static void CriarPlanilhaDistancia(ExcelPackage excel, List<Registro> dados)
    {
        var ws = excel.Workbook.Worksheets.Add("Distancia Total");

        // Usa o primeiro tracker (por ordem alfabética de ID) como referência de tempo
        var trackerRef = dados.GroupBy(d => d.Id).OrderBy(g => g.Key).First().ToList();

        ws.Cells[1, 1].Value = "Tempo (s)";
        ws.Cells[1, 2].Value = "Dist. Total";
        EstilizarCabecalho(ws.Cells[1, 1, 1, 2]);

        int linha = 2;
        foreach (var d in trackerRef)
        {
            ws.Cells[linha, 1].Value = d.Tempo;
            ws.Cells[linha, 2].Value = d.DistanciaTotal;
            linha++;
        }

        ws.Cells.AutoFitColumns();

        var chart = CriarGraficoBase(ws, "graficoDistancia",
            "Distancia Total x Tempo", eChartType.Line, 1, 4, 900, 450);

        var serie = chart.Series.Add(
            ws.Cells[2, 2, linha - 1, 2],
            ws.Cells[2, 1, linha - 1, 1]);
        serie.Header = "Distancia Total";

        chart.XAxis.Title.Text = "Tempo (s)";
        chart.YAxis.Title.Text = "Distancia (u.a.)";
    }

    // ─────────────────────────────────────────────
    // 3. Planilha PASSOS
    // ─────────────────────────────────────────────
    private static void CriarPlanilhaPassos(ExcelPackage excel, List<Registro> dados)
    {
        var ws = excel.Workbook.Worksheets.Add("Passos");

        // Agrupa por tempo e soma passo de todos os trackers
        var porTempo = dados
            .GroupBy(d => d.Tempo)
            .Select(g => new { Tempo = g.Key, Passo = g.Sum(x => x.DistanciaPasso) })
            .OrderBy(x => x.Tempo)
            .ToList();

        ws.Cells[1, 1].Value = "Tempo (s)";
        ws.Cells[1, 2].Value = "Dist. Passo";
        EstilizarCabecalho(ws.Cells[1, 1, 1, 2]);

        int linha = 2;
        foreach (var p in porTempo)
        {
            ws.Cells[linha, 1].Value = p.Tempo;
            ws.Cells[linha, 2].Value = p.Passo;
            linha++;
        }

        ws.Cells.AutoFitColumns();

        var chart = CriarGraficoBase(ws, "graficoPasso",
            "Distancia dos Passos", eChartType.ColumnClustered, 1, 4, 900, 450);

        var serie = chart.Series.Add(
            ws.Cells[2, 2, linha - 1, 2],
            ws.Cells[2, 1, linha - 1, 1]);
        serie.Header = "Dist. Passo";

        chart.XAxis.Title.Text = "Tempo (s)";
        chart.YAxis.Title.Text = "Distancia angular";
    }

    // ─────────────────────────────────────────────
    // 4. Planilha de ROTAÇÃO por tracker
    // ─────────────────────────────────────────────
    private static void CriarPlanilhaRotacao(ExcelPackage excel, List<Registro> dados,
        int numTracker, string nomePlanilha)
    {
        var filtrado = dados
            .Where(d => d.Id.Contains("/trackers/" + numTracker + "/"))
            .OrderBy(d => d.Tempo)
            .ToList();

        if (filtrado.Count == 0)
        {
            UnityEngine.Debug.LogWarning("[GraphTracker] Sem dados para tracker " + numTracker + " (" + nomePlanilha + ")");
            return;
        }

        var ws = excel.Workbook.Worksheets.Add(nomePlanilha);

        ws.Cells[1, 1].Value = "Tempo (s)";
        ws.Cells[1, 2].Value = "Rot X";
        ws.Cells[1, 3].Value = "Rot Y";
        ws.Cells[1, 4].Value = "Rot Z";
        EstilizarCabecalho(ws.Cells[1, 1, 1, 4]);

        int linha = 2;
        foreach (var d in filtrado)
        {
            ws.Cells[linha, 1].Value = d.Tempo;
            ws.Cells[linha, 2].Value = d.RotX;
            ws.Cells[linha, 3].Value = d.RotY;
            ws.Cells[linha, 4].Value = d.RotZ;

            if (linha % 2 == 0)
                AplicarZebra(ws.Cells[linha, 1, linha, 4]);

            linha++;
        }

        ws.Cells.AutoFitColumns();

        string nomeGrafico = "grafico" + nomePlanilha.Replace(" ", "");
        var chart = CriarGraficoBase(ws, nomeGrafico,
            "Rotacao – " + nomePlanilha, eChartType.Line, 1, 6, 900, 450);

        string[] headers = { "Rot X", "Rot Y", "Rot Z" };
        for (int c = 0; c < 3; c++)
        {
            var serie = chart.Series.Add(
                ws.Cells[2, c + 2, linha - 1, c + 2],
                ws.Cells[2, 1, linha - 1, 1]);
            serie.Header = headers[c];
        }

        chart.XAxis.Title.Text = "Tempo (s)";
        chart.YAxis.Title.Text = "Rotacao (graus)";
        chart.Legend.Position = eLegendPosition.Bottom;
    }

    // ─────────────────────────────────────────────
    // 5. Planilha COMPARAÇÃO
    // ─────────────────────────────────────────────
    private static void CriarPlanilhaComparacao(ExcelPackage excel, List<Registro> dados)
    {
        var ws = excel.Workbook.Worksheets.Add("Comparacao Trackers");

        var grupos = dados.GroupBy(d => d.Id).OrderBy(g => g.Key).ToList();
        var tempos = dados.Select(d => d.Tempo).Distinct().OrderBy(t => t).ToList();

        // Cabeçalho dinâmico
        ws.Cells[1, 1].Value = "Tempo (s)";
        int col = 2;
        foreach (var g in grupos)
        {
            string nome = NomeAmigavel(g.Key);
            ws.Cells[1, col].Value = nome + " – RotX";
            ws.Cells[1, col + 1].Value = nome + " – RotY";
            ws.Cells[1, col + 2].Value = nome + " – RotZ";
            col += 3;
        }
        EstilizarCabecalho(ws.Cells[1, 1, 1, col - 1]);

        // OTIMIZAÇÃO GC: Usando tupla estrutura nativa em vez de alocar chaves em strings continuas
        var idx = new Dictionary<(string, double), Registro>();
        foreach (var d in dados)
        {
            var key = (d.Id, d.Tempo);
            if (!idx.ContainsKey(key)) idx[key] = d;
        }

        int linha = 2;
        foreach (double t in tempos)
        {
            ws.Cells[linha, 1].Value = t;
            int c = 2;
            foreach (var g in grupos)
            {
                var key = (g.Key, t);
                if (idx.TryGetValue(key, out var reg))
                {
                    ws.Cells[linha, c].Value = reg.RotX;
                    ws.Cells[linha, c + 1].Value = reg.RotY;
                    ws.Cells[linha, c + 2].Value = reg.RotZ;
                }
                c += 3;
            }

            if (linha % 2 == 0)
                AplicarZebra(ws.Cells[linha, 1, linha, col - 1]);

            linha++;
        }

        ws.Cells.AutoFitColumns();

        // Gráfico comparando RotY de todos os trackers
        var chart = CriarGraficoBase(ws, "graficoComparacao",
            "Comparacao RotY – Todos os Trackers", eChartType.Line, 1, col, 1000, 500);

        int sCol = 2;
        foreach (var g in grupos)
        {
            var serie = chart.Series.Add(
                ws.Cells[2, sCol + 1, linha - 1, sCol + 1],
                ws.Cells[2, 1, linha - 1, 1]);
            serie.Header = NomeAmigavel(g.Key);
            sCol += 3;
        }

        chart.XAxis.Title.Text = "Tempo (s)";
        chart.YAxis.Title.Text = "Rot Y (graus)";
        chart.Legend.Position = eLegendPosition.Bottom;
    }

    // ─────────────────────────────────────────────
    // 6. DASHBOARD com estatísticas
    // ─────────────────────────────────────────────
    private static void CriarDashboard(ExcelPackage excel, List<Registro> dados)
    {
        var ws = excel.Workbook.Worksheets.Add("Dashboard");

        // Título principal
        ws.Cells["A1"].Value = "GRAPH TRACKER – Dashboard de Analise";
        ws.Cells["A1:L1"].Merge = true;
        ws.Cells["A1"].Style.Font.Size = 16;
        ws.Cells["A1"].Style.Font.Bold = true;
        ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(HexParaColor(COR_AZUL_ESCURO));
        ws.Cells["A1"].Style.Font.Color.SetColor(HexParaColor(COR_BRANCO));

        ws.Cells["A2"].Value = "Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        ws.Cells["A2:L2"].Merge = true;
        ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Cells["A2"].Style.Font.Italic = true;

        // Resumo geral
        int r = 4;
        ws.Cells[r, 1, r, 4].Merge = true;
        ws.Cells[r, 1].Value = "Resumo Geral";
        EstilizarCabecalho(ws.Cells[r, 1, r, 4]);
        r++;

        var grupos = dados.GroupBy(d => d.Id).OrderBy(g => g.Key).ToList();
        double tempoTotal = dados.Max(d => d.Tempo) - dados.Min(d => d.Tempo);

        EscreverPar(ws, r++, 1, "Total de registros", dados.Count);
        EscreverPar(ws, r++, 1, "Trackers ativos", grupos.Count);
        EscreverPar(ws, r++, 1, "Duracao da sessao (s)", tempoTotal, "0.00");
        EscreverPar(ws, r++, 1, "Freq. media de amostragem (Hz)", (dados.Count / Math.Max(tempoTotal, 1)), "0.0");
        r++;

        // Tabela de estatísticas
        ws.Cells[r, 1, r, 12].Merge = true;
        ws.Cells[r, 1].Value = "Estatisticas por Tracker";
        EstilizarCabecalho(ws.Cells[r, 1, r, 12]);
        r++;

        string[] cab = {
            "Tracker", "N", "Tempo Min (s)", "Tempo Max (s)",
            "RotX Media", "RotX Desvpad", "RotX Min", "RotX Max",
            "RotY Media", "RotY Desvpad",
            "RotZ Media", "RotZ Desvpad"
        };

        for (int c = 0; c < cab.Length; c++)
            ws.Cells[r, c + 1].Value = cab[c];

        ws.Cells[r, 1, r, cab.Length].Style.Font.Bold = true;
        ws.Cells[r, 1, r, cab.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[r, 1, r, cab.Length].Style.Fill.BackgroundColor.SetColor(HexParaColor(COR_AZUL_CLARO));
        r++;

        foreach (var g in grupos)
        {
            var lista = g.ToList();
            var rxList = lista.Select(d => d.RotX).ToList();
            var ryList = lista.Select(d => d.RotY).ToList();
            var rzList = lista.Select(d => d.RotZ).ToList();

            ws.Cells[r, 1].Value = NomeAmigavel(g.Key);
            ws.Cells[r, 2].Value = lista.Count;

            ws.Cells[r, 3].Value = lista.Min(d => d.Tempo);
            ws.Cells[r, 3].Style.Numberformat.Format = "0.00";

            ws.Cells[r, 4].Value = lista.Max(d => d.Tempo);
            ws.Cells[r, 4].Style.Numberformat.Format = "0.00";

            // Atribuição de valor puramente numérico com formatação explícita do Excel
            double[] valores = {
                Media(rxList), DesvioPadrao(rxList), rxList.Min(), rxList.Max(),
                Media(ryList), DesvioPadrao(ryList),
                Media(rzList), DesvioPadrao(rzList)
            };

            for (int i = 0; i < valores.Length; i++)
            {
                ws.Cells[r, 5 + i].Value = valores[i];
                ws.Cells[r, 5 + i].Style.Numberformat.Format = "0.0000";
            }

            if (r % 2 == 0)
                AplicarZebra(ws.Cells[r, 1, r, 12]);

            r++;
        }

        ws.Cells.AutoFitColumns();
        ws.View.FreezePanes(5, 1);
    }

    // ─────────────────────────────────────────────
    // Helpers estatísticos
    // ─────────────────────────────────────────────
    private static double Media(List<double> v) =>
        v.Count == 0 ? 0 : v.Average();

    private static double DesvioPadrao(List<double> v)
    {
        if (v.Count < 2) return 0;
        double m = v.Average();
        double soma = v.Sum(x => (x - m) * (x - m));
        return Math.Sqrt(soma / (v.Count - 1));
    }

    private static void EscreverPar(ExcelWorksheet ws, int linha, int col, string chave, object valor, string formato = null)
    {
        ws.Cells[linha, col].Value = chave;
        ws.Cells[linha, col].Style.Font.Bold = true;
        ws.Cells[linha, col + 1].Value = valor;
        if (!string.IsNullOrEmpty(formato))
            ws.Cells[linha, col + 1].Style.Numberformat.Format = formato;
    }
}
