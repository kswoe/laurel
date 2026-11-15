# Laurel

*Conquistas ganhas, nunca dadas de graça.*

**Outros idiomas:** [Español](../LEEME.md) · [English](README.en.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Русский](README.ru.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

Prepara seus jogos para que o **Hydra Launcher registre as conquistas quando você as consegue jogando**.
O app nunca desbloqueia conquistas por conta própria.

## Como usar
1. Abra `App\Laurel.exe` e **escolha seu idioma**. São 8: espanhol, inglês, português, francês, alemão, russo, chinês e japonês. Se marcar "Não perguntar novamente", você pode mudar depois com o botão **Idioma**.
2. Na primeira vez, o app procura sozinho a pasta de downloads do Hydra (só lê os dados, não os altera). Se seus jogos estiverem em outro lugar, clique em **Adicionar…**.
3. Digite sua **chave da API Steam**. É grátis e você a obtém pelo link "Obter chave". Sem ela, o app pode verificar seus jogos, mas não prepará-los. **A chave nunca é salva:** fica só na memória, e você terá que digitá-la sempre que abrir o app.
4. Clique em **Preparar jogos reparáveis**.
5. Deixe-o aberto ou minimize-o (ele fica ao lado do relógio). Quando você instalar um jogo, ele espera a instalação terminar e o prepara sozinho.

**Dica:** os cartões coloridos acima da lista contam os jogos de cada status; clique em um para ver só esses jogos. A busca filtra por nome, emulador ou AppID.

## O que significa cada status
| Status | Significado |
|---|---|
| OK | O Hydra já encontra o arquivo de conquistas desse jogo |
| PRONTO | Configurado; o arquivo aparecerá com sua primeira conquista |
| STEAM | A `steam_api.dll` é a original da Valve (assinatura verificada): o Hydra lê as conquistas da sua conta Steam |
| REPARÁVEL | Usa Goldberg, mas falta a lista de conquistas; o app pode criá-la |
| SEM APPID | Clique com o botão direito no jogo → "Definir AppID" |
| NÃO SUPORTADO | A DLL não é da Valve e não contém assinaturas de emuladores conhecidos: o emulador não salva conquistas onde o Hydra procura |
| SEM STEAM | Não usa a Steam. O app indica qual plataforma usa: GOG (com ou sem conquistas), Epic, Ubisoft, Microsoft Store ou nenhuma |

## Como protege seus arquivos
- **Nunca sobrescreve** um arquivo que já existe: só cria os que faltam.
- **Nunca apaga** nada por conta própria.
- Registra cada arquivo que cria em `cambios.json`.
- **Desfazer alterações do app** envia para a Lixeira só os arquivos criados pelo app que ninguém alterou depois. Se você ou o jogo os modificaram, eles são mantidos.
- O **modo simulação** mostra o que faria sem gravar nada.

## Identificador de DLL
Quando um jogo tem `steam_api.dll` mas nenhum arquivo de configuração conhecido, o app examina a DLL **em modo somente leitura**:
1. Verifica a assinatura digital com o Windows, sem se conectar à internet. Se a assinatura for da Valve e válida, o jogo é uma cópia da Steam.
2. Procura dentro do arquivo assinaturas de emuladores conhecidos: Goldberg, CODEX, RUNE, EMPRESS, TENOKE, OnlineFix, CreamAPI, SmartSteamEmu, 3DM, ALI213, SKIDROW, RLD! e Razor1911.

O resultado aparece em "Ver detalhes" (clique com o botão direito no jogo).

## Pontes de conquistas
Alguns jogos registram conquistas, mas em um lugar que o Hydra não lê. Com **Pontes de conquistas** ativadas, o app copia essas conquistas **já obtidas** para o formato que o Hydra entende (`%AppData%\GSE Saves\<AppID>\achievements.json`). Nunca inventa conquistas.
- **GOG Galaxy → Hydra:** para jogos GOG com conquistas. Defina o AppID da Steam (clique direito → "Definir AppID"). O app lê uma *cópia temporária* do banco de dados do GOG Galaxy, nunca o original. Se você digitou sua chave de API, só copia as conquistas cujos nomes coincidem com os da Steam.
- **RLE → Hydra:** o Hydra conhece a pasta do RLE (`%AppData%\RLE`), mas não a verifica; a ponte cobre isso.
- Se o arquivo de destino já existir e não tiver sido criado pelo app (por exemplo, porque o jogo usa Goldberg), ele **não é alterado**.
- Tudo o que uma ponte cria fica registrado em `cambios.json` e pode ser desfeito.

## Guia para jogos NÃO SUPORTADOS
O app **não instala emuladores**. Para esses jogos, clique direito → **"Guia para acompanhar as conquistas…"** abre um guia passo a passo:
1. **Backup** da DLL atual (como `.hydralogros-original`, sem sobrescrever nada).
2. Link para o repositório oficial do gbe_fork.
3. Abre a pasta da DLL para que **você** a substitua.
4. **Verificar:** se detectar o gbe_fork, prepara o jogo automaticamente.

Oferecido apenas para cópias cuja DLL já não é a original da Valve.

## Privacidade e uso da API Steam
- **O que é salvo no disco:** só as bibliotecas, as opções (incluindo o idioma), os AppIDs que você definir e a lista de arquivos criados (para poder desfazer). Nunca a chave de API. Se um `config.json` antigo a tinha, o app a remove ao iniciar.
- **O registro não guarda dados pessoais:** em `registro.txt` e na tela, sua pasta de usuário aparece como `%USERPROFILE%`, o nome de usuário como `<usuário>`, e e-mails e a chave ficam ocultos.
- **O app consulta a Steam com cuidado** para que o uso da sua chave pareça normal:
  - uma consulta por vez, com pelo menos 3 segundos entre elas;
  - no máximo 150 consultas por sessão;
  - cada jogo é consultado uma só vez por sessão;
  - se a Steam rejeitar a chave, ela não é usada de novo até você digitar outra;
  - se a Steam pedir para diminuir o ritmo, as consultas pausam por 15 minutos.
- A lista de conquistas é baixada da Steam no idioma que você escolheu.

## Segurança e verificação
**O que verifica:** só as subpastas das suas bibliotecas, procurando apenas os arquivos que identificam emuladores e plataformas. Não entra em pastas de áudio, vídeo, recursos nem instaladores, e oculta as pastas sem arquivos de jogo. Verifica vários jogos ao mesmo tempo, com limite de tamanho e de tempo por jogo.

**Travas:**
- **Bibliotecas:** não é possível usar como biblioteca a raiz de um disco, Windows, Arquivos de Programas, ProgramData, Usuários, seu perfil, AppData nem a pasta temporária. Nem links simbólicos. Se o `config.json` os contiver, são ignorados.
- **Links:** nunca segue links simbólicos nem *junctions*, nem lê arquivos do OneDrive que só estão na nuvem (não os baixa).
- **Gravação:** só pode criar `steam_settings\achievements.json`, `steam_settings\steam_appid.txt`, o backup `.hydralogros-original` e `GSE Saves\<AppID>\achievements.json`. Qualquer outro caminho é bloqueado.
- **Desfazer:** mesmo que alguém edite o `cambios.json`, só atua sobre esses mesmos arquivos.
- **Rede:** só se conecta a `api.steampowered.com` via HTTPS e não segue redirecionamentos, então sua chave não pode parar em outro servidor. Só abre dois sites confiáveis.
- **Nada é executado:** nunca inicia programas das pastas de jogos. As pastas são abertas com o Explorador e as DLLs do Windows só são carregadas de `System32`.
- **Dados de entrada:** os AppIDs só podem ser números, arquivos de texto acima de 1 MB são ignorados e as buscas de texto têm tempo máximo.
- **Registro:** o `registro.txt` é rotacionado ao chegar a 2 MB.
- **Aviso** se você abrir o app como administrador (não é necessário).

## Arquivos da pasta
- `App\Laurel.exe`: o aplicativo. Requer o .NET 10 Desktop Runtime.
- `App\config.json`, `cambios.json`, `estado.json` e `registro.txt`: criados pelo app durante o uso.
- `leeme\`: este documento em outros idiomas.
- `src\`: código-fonte. Para recompilar: `dotnet publish src -c Release -o App`.
- `tools\IconGen\`: gera o ícone (`src\app.ico`). Para regenerá-lo: `dotnet run --project tools/IconGen -- src/app.ico`.
- `Portable\Laurel.exe`: versão portátil. Traz o .NET embutido (não precisa instalar nada) e guarda seus dados na própria pasta. Para recompilar: `dotnet publish src -c Release -p:Portable=true -o Portable`.
- O botão **Leia-me** do app mostra este documento no seu idioma. Ele usa uma cópia incluída no `.exe`: editar estes `.md` não muda o que o app mostra (é preciso recompilar).

## Licença
O Laurel é software livre: você pode usá-lo, estudá-lo, modificá-lo e compartilhá-lo nos termos da [Licença Pública Geral GNU v3.0](../LICENSE) (GPL-3.0). Se distribuir uma versão modificada, ela também deve ser livre e vir com o código-fonte.

*** ESTA FERRAMENTA É DE USO LIVRE ***
