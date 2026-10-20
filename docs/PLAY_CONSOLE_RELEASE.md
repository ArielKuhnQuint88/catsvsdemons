# Cats vs Demons — exportação Android

## Estado desta revisão

Melhorias implementadas no código: iluminação de personagens com sombras e neblina,
água animada, partículas das estações, tratamento de cor e HUD animado. Os quatro
cenários de build incluem o menu e toda a campanha de 12 fases. O auto-rig de Kin
e dos demônios está ativado e mantém a leitura das malhas necessária à animação.

**A produção ainda depende de compilação na Unity, teste visual, teste em Android e
geração do AAB assinado.** A inspeção de código e os testes do verificador de AAB
não substituem esses testes. Esta revisão não publica o jogo.

## Abrir o projeto certo

Abra a **raiz do repositório** no Unity Hub: a pasta que contém `Assets/_Project`,
`Packages` e `ProjectSettings`. A pasta interna `CatsVsDemons` é uma cópia antiga
do template e não contém o jogo atualizado. Use **Unity 6000.5.4f1**, indicada em
`ProjectSettings/ProjectVersion.txt`.

No Unity Hub, instale Android Build Support, Android SDK & NDK Tools e OpenJDK
para essa versão. Instale também o SDK Android 36 se a Unity solicitar.

## Exportar no PC com dois cliques

### APK para instalar e testar no celular

Com a Unity fechada, execute **EXPORTAR_APK.cmd** na raiz do projeto.
Ele gera um APK com a chave de desenvolvimento da Unity, sem solicitar a chave
de upload nem suas senhas. O aplicativo aparece como **Cats vs Demons (Teste)**,
com pacote `com.qipgames.catsvsdemons.test`, podendo coexistir com o jogo da loja.
As configurações de identificação e assinatura anteriores são restauradas no
fim da exportação, inclusive em falhas. Os dados e a progressão desse app de
teste são próprios.

A saída fica em `Builds/CatsVsDemons-Android/CatsVsDemons-Teste-<data>-<id>.apk`.
Cada execução usa um nome novo. O lançador confere o relatório e o hash SHA-256,
e usa `apksigner verify` do SDK para verificar a assinatura antes de abrir a pasta.
O inspetor também confere os arquivos obrigatórios, ARM64 e alinhamento ELF de
16 KB de todas as bibliotecas nativas. A instalação e a jogabilidade ainda
precisam ser testadas no aparelho. Logs: `apk-build.log` e `apk-signature-check.log`.

Copie o `.apk` para o Android e abra-o. Autorize a instalação pelo aplicativo
usado para abrir esse arquivo, caso o sistema solicite. Esta versão usa Android
8 ou superior e processador ARM64. O APK de teste é destinado à instalação
direta; a publicação no Play Console continua pelo fluxo AAB abaixo.

Também pode usar **Tools > Cats vs Demons > Exportar APK de teste (Android)**
no Editor. A verificação criptográfica com `apksigner` é feita pelo lançador.

### AAB para o Play Console

Com o projeto atualizado e a Unity fechada, execute **EXPORTAR_PLAY_CONSOLE.cmd**
na raiz do projeto. O lançador localiza a versão da Unity indicada no projeto,
confere o módulo Android/OpenJDK e pede o maior **Bundle Version Code já enviado**
ao Console (digite 0 se nunca enviou). Ele escolhe um código superior ao enviado,
sem reduzir o código do projeto.

Selecione a chave de upload **ativa no Play Console** e digite as senhas nos
campos ocultos. O seletor sempre abre, mesmo quando existe uma chave antiga em
Documentos/Chaves. O alias padrão é `catsvsdemons`; o caminho selecionado aparece
antes dos pedidos de senha.

Antes de iniciar a Unity, o Java assina e verifica um pequeno arquivo de teste.
Senha errada, alias ausente ou chave sem acesso interrompem a exportação nessa
etapa, preservando o keystore. Isso evita esperar a compilação inteira para
descobrir que a assinatura não funciona.

O lançador entrega as credenciais por um arquivo temporário com acesso restrito
à conta atual do Windows. A Unity o lê e apaga antes de compilar. O lançador também
remove a pasta temporária ao terminar, inclusive em falhas. As senhas não são
passadas em argumentos nem no ambiente herdado pela Unity/Gradle. O log final
também recebe uma limpeza das credenciais de assinatura reconhecidas.

O lançador executa o exportador da Unity, confere o relatório e o SHA-256, verifica
a assinatura criptográfica com o `jarsigner` do OpenJDK e abre a pasta do AAB.
Um arquivo anterior com o mesmo nome é preservado. O build nativo e os testes em
Android continuam necessários; este script não substitui nem simula um AAB.

Se a Unity ou a chave estiverem em outro local:

```powershell
.\Tools\Android\Export-PlayConsole.ps1 `
  -UnityPath 'D:\Unity\6000.5.4f1\Editor\Unity.exe' `
  -KeystorePath 'C:\minhas-chaves\catsvsdemons-upload.keystore' `
  -KeyAlias 'catsvsdemons' -LastUploadedVersionCode 2
```

A saída fica em `Builds/CatsVsDemons-PlayStore`. Envie o **.aab** para **Teste interno**
no Play Console e valide no aparelho. O lançador não envia nem publica uma versão
no Console automaticamente.

## Exportar na Unity

1. Aguarde a importação e confirme que o Console não tem erros de compilação.
2. Execute **Tools > Cats vs Demons > Preparar Android para Play Console**.
3. Em **Player > Android > Publishing Settings**, selecione a chave de upload
   **ativa no Play Console** e informe as duas senhas. Se houve redefinição por
   senha esquecida, use o novo keystore apenas depois da ativação confirmada pelo
   Google. O alias usado pelo assistente de nova chave é `catsvsdemons`, com a
   mesma senha nova nos campos do arquivo e do alias.
4. Em **Player > Android > Other Settings**, confira `Bundle Version Code`:
   a configuração base usa **2**, que precisa ser maior que todo código já enviado.
   Aumente-o se necessário. O identificador permanece
   **com.qipgames.catsvsdemons**, e a versão é **1.0.2** (código base **3**).
5. Execute **Tools > Cats vs Demons > Exportar para Play Store (AAB)**.

O exportador configura **IL2CPP, ARM64, API 36, orientação horizontal e build de
release**, preserva os shaders runtime e a leitura das malhas animadas, e inclui:

- `MainMenu`
- `Game` (fases 1–4)
- `Game_BambooGrove` (fases 5–8)
- `Game_EclipseSanctuary` (fases 9–12)

A saída fica em `Builds/CatsVsDemons-PlayStore/CatsVsDemons-v1-0-1-2.aab`
(o nome acompanha versão e código). O arquivo `.build.json` ao lado informa
SHA-256, cenas, versão e verificações realizadas. Um AAB existente com o mesmo
nome é preservado; mova-o ou aumente o código para uma nova exportação.

O exportador na Unity verifica a estrutura do bundle, a presença da assinatura de upload,
o formato ELF64 ARM64 e o alinhamento de 16 KB dos segmentos LOAD de **todas as
bibliotecas nativas incluídas**, inclusive plugins. Isso não verifica a validade
criptográfica da assinatura por si só (o lançador do PC usa `jarsigner` para isso),
não valida o manifest protobuf e não confirma se a
chave corresponde à cadastrada no Play Console. Se uma biblioteca falhar no
alinhamento, atualize a Unity ou o plugin apontado e refaça o build.

## Verificações antes de enviar

- Jogue a partida em Android: andar, atacar, construir cada torre, usar o golpe
  especial e entrar na loja pelo computador.
- Confira barras, rastro de dano, moeda, contagem regressiva, toque nos botões,
  notch/safe area, joystick, zoom com dois dedos, pausa e retorno após minimizar.
- Teste os saltos das fases **4 → 5** e **8 → 9**: moedas e roupas devem continuar.
- Confira vitória e derrota, reinício e áudio. O HUD deve animar mesmo pausado.
- Observe iluminação, materiais, água e partículas nos três cenários e nas quatro
  estações. Confira memória e FPS com o Profiler em um aparelho de referência.
- Envie primeiro para **Teste interno** no Play Console e confira o relatório de
  pré-lançamento, suporte a 16 KB, assinatura e compatibilidade.
- Complete os dados do app e da loja no Console conforme o conteúdo real do jogo.

## Exportação por linha de comando

Prefira `Tools/Android/Export-PlayConsole.ps1`, que valida a chave e prepara o
transporte temporário protegido. O método da Unity também funciona em batch mode,
em uma máquina licenciada com Android Build Support. `CVD_SIGNING_FILE` indica
um JSON temporário protegido contendo `keystorePath`, `keyAlias`,
`keystorePassword` e `keyPassword`; ele é consumido e apagado ao entrar no método.
Não crie esse arquivo dentro do repositório. `CVD_VERSION_CODE` escolhe o código
da versão e rejeita números inválidos ou inferiores ao projeto.

Por compatibilidade, as variáveis antigas abaixo ainda são aceitas. O método
remove as duas senhas do ambiente imediatamente após a leitura. Use o lançador
atualizado para evitar que processos iniciados durante a abertura da Unity
recebam essas variáveis. Não grave senhas em scripts, logs ou arquivos versionados:

```text
CVD_KEYSTORE_PATH
CVD_KEY_ALIAS
CVD_KEYSTORE_PASSWORD
CVD_KEY_PASSWORD
CVD_VERSION_CODE
```

Se a senha foi esquecida e o app usa a Assinatura de apps do Google Play, gere uma
nova chave de upload e solicite a redefinição com o certificado público `.pem`.
Aguarde a confirmação e a data de ativação informadas pelo Console. Preserve o
nome do pacote do aplicativo. Consulte as
[instruções oficiais de redefinição](https://support.google.com/googleplay/android-developer/answer/9842756?hl=pt-br).

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe' `
  -batchmode -quit -buildTarget Android -projectPath 'C:\caminho\catsvsdemons' `
  -executeMethod CatsVsDemons.Editor.PreparePlayableBuild.ExportPlayStoreBundle `
  -logFile 'C:\caminho\catsvsdemons\Builds\play-console-build.log'
```

## Teste independente do verificador

Com .NET 8 instalado:

```sh
dotnet run --project Tools/Validation/BundleInspectorChecks.csproj
```

O teste compila o mesmo código usado pelo exportador e verifica bundles sintéticos
com bibliotecas alinhadas, não alinhadas, truncadas, de outra arquitetura e sem
assinatura ou manifest. Para verificar um bundle real:

```sh
dotnet run --project Tools/Validation/BundleInspectorChecks.csproj -- caminho/do/jogo.aab
```

## Requisitos consultados

- [API alvo exigida pelo Google Play](https://support.google.com/googleplay/android-developer/answer/11926878)
- [Bibliotecas Android e páginas de 16 KB](https://developer.android.com/guide/practices/page-sizes)

Política de API consultada em 14/10/2026: novas submissões e atualizações
de apps Android comuns exigem API 36 ou superior desde 31/08/2026.
Consulte o Console para exigências da conta, testes e publicação.
