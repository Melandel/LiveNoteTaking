# LiveNoteTakingDemo

For a table of content, we thought it better to check your web browser or its plugins!

## Standard Markdown syntax
Lorem ipsum dolor sit amet, consectetur adipiscing elit.

Aenean `pulvinar` placerat sapien, nec dapibus tortor viverra condimentum.

Nulla [facilisi](https://github.com/vim/vim).

**Sed in interdum justo.**

_Donec fermentum elit sed mi pellentesque, eget feugiat diam molestie._

> Donec ornare tellus et aliquet efficitur.

* Aliquam nec fermentum nisl.
* Mauris sed ultrices massa, a elementum diam.
* Fusce ligula mi, semper eu viverra sed, efficitur non odio.


Vivamus molestie consectetur tellus, gravida vulputate lectus condimentum nec. Pellentesque tincidunt fringilla diam, vel rhoncus est sodales at.

* [ ] do not talk about Fight Club
* [x] do NOT talk about Fight Club.


| toto | tata |
|--|--|
|2 | 42 |

```cs
public class Hello
{
  public static void Main()
  {
    System.Console.WriteLine("Hello, World!");
    System.Console.WriteLine("Rock all night long!");
  }
}
```

## Vertical columns with titles

{| foobar
```cs
public class MyClassWithInt
{
  public int Value { get; set; }
}
```
.| baz
```cs
public class MyClassWithString
{
  public string Value { get; set; }
}
```
|}

## Data rows

{|
```data
{ "PrimaryKey": "toto", "Column1": "tata", "Column2": "tutu" }
```

.|
```data
[ { "PrimaryKey": "toto", "Column1": "tata", "Column2": "tutu" } ]
```

.|
```data
[]
```

.|

```data
{ "count": 2091, "_": [
  { "Column1": "toto", "Column2": "tata", "Column3": 1234, "Column4": "tata", "Column5": "toto", "Column6": "tata", "Column7": "toto", "Column8": "tata", "Column9": "toto", "Column10": "tata", "Column11": "toto", "Column12": "tata", "Column13": "toto", "Column14": "tata", "Column15": "toto", "Column16": "tata", "Column17": "toto", "Column18": "tata", "Column19": "toto", "Column20": "tata" },
  { "Column1": "foo", "Column2": "bar", "Column3": "1234", "Column4": "true", "Column5": "foo", "Column6": "bar", "Column7": "foo", "Column8": "bar", "Column9": "foo", "Column10": "bar", "Column11": "foo", "Column12": "bar", "Column13": "foo", "Column14": "bar", "Column15": "foo", "Column16": "bar", "Column17": "foo", "Column18": "bar", "Column19": "foo", "Column20": "bar" },
  { "Column1": "qux", "Column2": "quux", "Column3": "qux", "Column4": true, "Column5": "qux", "Column6": "quux", "Column7": "qux", "Column8": "quux", "Column9": "qux", "Column10": "quux", "Column11": "qux", "Column12": "quux", "Column13": "qux", "Column14": "quux", "Column15": "qux", "Column16": "quux", "Column17": "qux", "Column18": "quux", "Column19": "qux", "Column20": "quux" },
]}
```

|}

## Weighted vertical columns

{|12 A lorem ipsum

Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed in interdum justo. Donec fermentum elit sed mi pellentesque, eget feugiat diam molestie. Aliquam nec fermentum nisl. Mauris sed ultrices massa, a elementum diam. Fusce ligula mi, semper eu viverra sed, efficitur non odio. Nulla facilisi. Donec ornare tellus et aliquet efficitur. Aenean pulvinar placerat sapien, nec dapibus tortor viverra condimentum. Vivamus molestie consectetur tellus, gravida vulputate lectus condimentum nec. Pellentesque tincidunt fringilla diam, vel rhoncus est sodales at.  Integer et purus eu purus maximus cursus condimentum sit amet tortor. Aenean tincidunt tempus tortor. Suspendisse vel nibh nulla. Duis ut leo non arcu sollicitudin pharetra eu ut arcu. Donec eleifend tristique tortor sed finibus. Integer ac molestie purus. Ut eros velit, tincidunt sed magna quis, fermentum condimentum felis.  Vestibulum cursus, eros ac sodales malesuada, ligula purus mattis eros, a bibendum diam risus eu lorem. Nam ullamcorper at lorem id eleifend. Etiam rhoncus nec velit vel ornare. Curabitur ultrices mollis vulputate. Nulla a nisl libero. Vestibulum tincidunt justo sit amet faucibus iaculis. Maecenas interdum dui quis massa viverra finibus. Donec ultrices felis id efficitur egestas.

Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed in interdum justo. Donec fermentum elit sed mi pellentesque, eget feugiat diam molestie. Aliquam nec fermentum nisl. Mauris sed ultrices massa, a elementum diam. Fusce ligula mi, semper eu viverra sed, efficitur non odio. Nulla facilisi. Donec ornare tellus et aliquet efficitur. Aenean pulvinar placerat sapien, nec dapibus tortor viverra condimentum. Vivamus molestie consectetur tellus, gravida vulputate lectus condimentum nec. Pellentesque tincidunt fringilla diam, vel rhoncus est sodales at.

.| An animated D2 diagram

```d2 
direction: right

title: {
  label: Normal deployment
  near: bottom-center
  shape: text
  style.font-size: 40
  style.underline: true
}

local: {
  code: {
    icon: https://icons.terrastruct.com/dev/go.svg
  }
}
local.code -> github.dev: commit

github: {
  icon: https://icons.terrastruct.com/dev/github.svg
  dev
  master: {
    workflows
  }

  dev -> master.workflows: merge trigger
}

github.master.workflows -> aws.builders: upload and run

aws: {
  builders -> s3: upload binaries
  ec2 <- s3: pull binaries

  builders: {
    icon: https://icons.terrastruct.com/aws/Developer%20Tools/AWS-CodeBuild_light-bg.svg
  }
  s3: {
    icon: https://icons.terrastruct.com/aws/Storage/Amazon-S3-Glacier_light-bg.svg
  }
  ec2: {
    icon: https://icons.terrastruct.com/aws/_Group%20Icons/EC2-instance-container_light-bg.svg
  }
}

local.code -> aws.ec2: {
  style.opacity: 0.0
}

scenarios: {
  hotfix: {
    title.label: Hotfix deployment
    (local.code -> github.dev)[0].style: {
      stroke: "#ca052b"
      opacity: 0.1
    }

    github: {
      dev: {
        style.opacity: 0.1
      }
      master: {
        workflows: {
          style.opacity: 0.1
        }
        style.opacity: 0.1
      }

      (dev -> master.workflows)[0].style.opacity: 0.1
      style.opacity: 0.1
      style.fill: "#ca052b"
    }

    (github.master.workflows -> aws.builders)[0].style.opacity: 0.1

    local.code -> aws.ec2: {
      style.opacity: 1
      style.stroke-dash: 5
      style.stroke: "#167c3c"
    }
  }
}
```

|}

{| A D2 diagram without animation


```d2 Junior Developer Lingo
hello

front {
  page d'accueil
  description des miels
  liste des produits
  panier
}

back {
  controller -> route
  base de données
}
```

.| Collapsible sections (3 levels of color)

{< Example 1 : Foo

{|462 input

```cs
public class C {
  public int I { get; set; }
}
```

* foo
* bar
* baz

.| output

```mmd
pie title Pets adopted by volunteers
    "Dogs" : 386
    "Cats" : 85
    "Rats" : 15
```

.| Conclusion
* foo
* bar
* foobar

|}

>}
{<< Example 2 : Bar

```puml totou
Bob -> Alice
```
>>}
{<<< Example 3 : Baz

```puml
Bob -> Alice
```
>>>}
{<<< Example 4 : Qux

```puml
Bob -> Alice
```
>>>}
{<<< Example 5 : Quux

```puml
Bob -> Alice
```
>>>}
|}


## When there are syntax errors in diagrams
```d2
domain {
  miel
  produits
  panier
}

fonctionnalités { saywhatnow{
  afficher liste produits
}
```

```mmd
pie title Pets adopted by volunteers
    "Dogs:/saywhatnow"" : 386
    "Cats" : 85
    "Rats" : 15
```

```puml
Boba -saywhatnow> Alissou
```

