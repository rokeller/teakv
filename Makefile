.PHONY: build
build:
	dotnet build TeaSuite.KV.sln

.PHONY: examples
examples:
	dotnet build Examples.sln

.PHONY: clean
clean:
	dotnet clean TeaSuite.KV.sln
	dotnet clean Examples.sln

.PHONY: restore
restore:
	dotnet restore --force-evaluate TeaSuite.KV.sln
	dotnet restore --force-evaluate Examples.sln

.PHONY: test
test:
	dotnet test TeaSuite.KV.sln

.PHONY: coverage
coverage:
	rm -rf TestResults/Temp
	dotnet test TeaSuite.KV.sln --collect:'XPlat Code Coverage' \
		--results-directory TestResults/Temp
	DOTNET_ROOT=/usr/share/dotnet reportgenerator \
		-reports:"TestResults/Temp/*/coverage.cobertura.xml"  \
		-targetdir:"coverage"                                 \
		-historydir:"coverage/history"                        \
		'-reporttypes:Html_Dark;MarkdownSummaryGithub'

.PHONY: pack
pack:
	dotnet pack TeaSuite.KV.sln --configuration Release --output packages/
