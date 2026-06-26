.PHONY: build
build:
	dotnet build TeaSuite.KV.slnx

.PHONY: examples
examples:
	dotnet build Examples.slnx

.PHONY: examples.run.shorturl
examples.run.shorturl:
	dotnet run --project examples/ShortUrl

.PHONY: clean
clean:
	dotnet clean TeaSuite.KV.slnx
	dotnet clean Examples.slnx

.PHONY: restore
restore:
	dotnet restore --force-evaluate TeaSuite.KV.slnx
	dotnet restore --force-evaluate Examples.slnx

.PHONY: test
test:
	dotnet test TeaSuite.KV.slnx

.PHONY: coverage
coverage:
	rm -rf TestResults/Temp
	dotnet test TeaSuite.KV.slnx --collect:'XPlat Code Coverage' \
		--results-directory TestResults/Temp
	reportgenerator \
		-reports:"TestResults/Temp/*/coverage.cobertura.xml"  \
		-targetdir:"coverage"                                 \
		-historydir:"coverage/history"                        \
		'-reporttypes:Html_Dark;MarkdownSummaryGithub'

.PHONY: pack
pack:
	dotnet pack TeaSuite.KV.slnx --configuration Release --output packages/
