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

Com o projeto atualizado e a Unity fechada, execute **EXPORTAR_PLAY_CONSOLE.cmd**
na raiz do projeto. O lançador localiza a versão da Unity indicada no projeto,
confere o módulo Android/OpenJDK e pede o maior **Bundle Version Code já enviado**
ao Console (digite 0 se nunca enviou). Ele escolhe um código superior ao enviado,
sem reduzir o código do projeto.

Selecione a chave de upload existente e digite as senhas nos campos ocultos.
A chave padrão é buscada em `Documentos/Chaves/catsvsdemons-upload.keystore`, com
alias `catsvsdemons`. As senhas passam apenas para o processo de build; depois,
o ambiente original é restaurado. Elas não entram nos argumentos nem em arquivos.

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
   **já usada no Play Console** e informe as duas senhas. No fluxo anterior, o
   arquivo local era `C:\Users\Qip\Documents\Chaves\catsvsdemons-upload.keystore`
   e o alias era `catsvsdemons`. Não crie outra chave para atualizar o mesmo app.
4. Em **Player > Android > Other Settings**, confira `Bundle Version Code`:
   a configuração base usa **2**, que precisa ser maior que todo código já enviado.
   Aumente-o se necessário. O identificador permanece
   **com.qipgames.catsvsdemons**, e a versão é **1.0.1**.
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

O método também funciona em batch mode, em uma máquina com Unity licenciada e
Android Build Support. As variáveis de assinatura abaixo são opcionais quando a chave já
foi informada no Editor. `CVD_VERSION_CODE` permite escolher o código da versão
no batch mode e rejeita números inválidos ou inferiores ao projeto. Use variáveis locais ou segredos do CI e não grave senhas
em scripts, logs ou arquivos versionados:

```text
CVD_KEYSTORE_PATH
CVD_KEY_ALIAS
CVD_KEYSTORE_PASSWORD
CVD_KEY_PASSWORD
CVD_VERSION_CODE
```

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
