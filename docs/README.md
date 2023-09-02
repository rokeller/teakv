# TeaSuite Key-Value Store

A simple in-process / embedded Key-Value store for .Net. Writes and deletes are
first made in-memory (so they are very fast). The data stored in-memory can
periodically be flushed to segments on disk / persistent storage. Segments are
stored in sorted order of the keys.
Once a segment has been written, it will never change, but it can get deleted
_after_ having been merged with other segments into a new segment. Each segment
consist of a data file and an index file.

When data is not found in-memory, the segments are searched by starting with the
most recent segment first. As a result, the more segments are accumulated, the
more segments need to be searched for entries that do not exist. Therefore, the
segments can be merged (aka compacted) so that reads in segments can be made
faster.
