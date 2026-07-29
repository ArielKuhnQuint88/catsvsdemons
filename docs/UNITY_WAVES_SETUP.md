# Criar ondas de teste

## Atualizar

1. GitHub Desktop → **Fetch origin**.
2. Clique em **Pull origin**.
3. Aguarde a Unity compilar.

## Adicionar ondas

Abra:

```text
Tools → Cats vs Demons → Add Test Waves
```

O protótipo original do demônio ficará desativado e será usado como modelo.

## Testar

Pressione **Play**.

Serão executadas:

- onda 1: 5 demônios;
- onda 2: 7 demônios;
- onda 3: 9 demônios.

Os demônios alternam entre os caminhos esquerdo, direito e inferior. Uma nova onda começa apenas depois que a anterior termina.

Se a casa chegar a zero, as ondas param. Se todas forem concluídas, o Console exibirá a mensagem de vitória.

## Salvar

Faça o commit:

```text
feat: add test enemy waves
```

Depois clique em **Push origin**.
