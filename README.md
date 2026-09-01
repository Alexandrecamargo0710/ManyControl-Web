# ManyControl-Web (PWA) 📱💸

Aplicação Web / PWA de **Controle Financeiro Pessoal**, construída com **.NET 10 Blazor WebAssembly**, projetada para funcionar com fluidez nativa no **iPhone (iOS)**, **Android** e em qualquer navegador Desktop/Mobile a **custo zero** de hospedagem via **GitHub Pages**.

---

## 🌟 Principais Recursos

- 📲 **Experiência Nativa no iPhone (PWA)**:
  - Adicione à Tela de Início e use em tela cheia (`standalone`), sem barras do Safari e com ícone personalizado.
  - Funciona **offline** através de Service Worker.
- 🔒 **Privacidade & Armazenamento Local ($0 de servidor)**:
  - Seus dados ficam salvos de forma privada no `LocalStorage` do próprio dispositivo.
- 🔄 **Compatibilidade Total de Backup**:
  - Exporte e importe o arquivo `manycontrol-sync.json` para trocar dados com as versões nativas do **ManyControl Windows (.exe)** e **Android (.apk)**.
- ⚡ **Gestão Financeira Completa**:
  - Painel de Resumo (Receitas, Despesas, Saldo Líquido, Pagas vs Pendentes).
  - Lançamento rápido de Receitas e Despesas com categorias.
  - Despesas Recorrentes que se repetem automaticamente a cada mês.
  - Extrato detalhado com busca textual em tempo real e filtros por Tipo e Status.
  - Gerenciamento personalizado de Categorias.
- 🚀 **Deploy Automático e Gratuito ($0)**:
  - Pipeline de CI/CD no GitHub Actions que publica o site no **GitHub Pages** a cada commit.

---

## 📲 Como Instalar no iPhone (iOS)

1. Acesse o link do ManyControl pelo **Safari**.
2. Toque no botão **Compartilhar** (ícone do quadrado com a seta para cima na barra inferior).
3. Role para baixo e selecione **"Adicionar à Tela de Início"**.
4. Toque em **Adicionar**.
5. **Pronto!** O ícone do ManyControl aparecerá na tela inicial do seu iPhone e abrirá como um app nativo.

---
   git push -u origin main
   ```
3. No GitHub, acesse: **Settings** > **Pages** > em **Build and deployment / Source**, selecione **Deploy from a branch** e escolha o branch **`gh-pages`** (pasta `/ (root)`).
4. O seu app estará online e acessível gratuitamente em `https://SEU_USUARIO.github.io/ManyControl-Web/`!
