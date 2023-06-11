# Example: URL Shortening Service

This example illustrates a very simple URL shortening service. Shortening is
kept very simple:

1. A "long" URL is needed as input.
2. A unique unsigned 64-bit ID is generated as follows:
    1. Take the current offset (in milliseconds) from 2023-06-01 00:00 UTC,
       multiply it by 1000.
    2. Increment a sequence number and take the remainder of the division of
       the incremented sequence number by 1000.
    3. Add the remainder.
3. Encode the unique ID using an encoding very similar to base-64 and use the
   output as the short ID. Use the ID (_not_ the short ID) as the key and the
   input URL as the value in the KV store.

This has two properties: it produces (with a high enough probability) unique IDs
that are monotonically increasing over time. Obviously with all of this in place
you won't have a full-blown and scalable solution for the internet, but
something that would already work quite well, e.g. on an internal network.

## APIs / Endpoints

The service has a few different endpoints:

1. Generate a short URL using a `POST` to `/api/shorten` with a body of type
   `application/x-www-form-urlencoded` having a single parameter called `url`.
2. Inspect a short URL using a `GET` to `/api/details/<short-id>`.
3. Redirect to the long URL using a `GET` to `/<short-id>`.

## Populate with data

If you quickly want to populate your local database with some URLs and short IDs,
here's an idea: Use the _top 1000 repos_ on GitHub, from
[top1000repos.com](https://top1000repos.com/). The page uses a REST API that you
can leverage too, and here is how. First, make sure the `ShortUrl` example is
running, then execute the following commands (bash):

```bash
curl -s 'https://api.top1000repos.com/repositories' |
    jq '.[].html_url' -r |
    while read line; do
        curl 'http://localhost:5276/api/shorten' --data-urlencode "url=$line"
        echo
    done
```
