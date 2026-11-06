# Lessons learned

Problems I hit while building this project, what caused them, and how I fixed them. Most of the real learning happened here.

## AWS and data

| Problem | Cause | Fix / lesson |
|---|---|---|
| Lambda kept rejecting new event types after I "deployed" a code change in the console | The console edit was never actually deployed; `LastModified` still showed the previous day | Deploy from the CLI with `aws lambda update-function-code` and verify with `get-function-configuration --query LastModified`. **Don't trust a deploy you haven't verified** |
| Glue crawler failed with "unable to assume role … enable STS" | The IAM role had just been created and hadn't propagated yet | Retry after a minute. New IAM roles are eventually consistent |
| New day's data invisible in Athena | Each new `day=` folder is a new partition, and the Glue catalog didn't know about it | First `MSCK REPAIR TABLE`, then replaced entirely with **partition projection**, so Athena calculates partitions from the folder pattern |
| Test events showed up as a fake level in the analysis | Manual test events written with a placeholder `level_id` | Deleted the test files and filtered them in the base view. Keep test data clearly labeled or out of production paths |
| Amazon Quick Sight sign-up page just reloaded | Quick Sight isn't available on the AWS free plan | Switched to Power BI over the Athena ODBC driver. Keeping logic in Athena views made the BI tool interchangeable |
| Simulated players arrived in one day's partition despite being backdated over two weeks | Lambda partitions by **arrival** time; the events carry their own **event** time | Analyze by event time in the views; partition filters select by arrival date. This is the classic event-time vs processing-time distinction |

## Analytics and dashboard

| Problem | Cause | Fix / lesson |
|---|---|---|
| Power BI: "a circular dependency was detected" when sorting levels | A DAX calculated column derived from `level_id` was used to sort `level_id` itself | Create the sort column in Power Query (or upstream in SQL) instead |
| A slicer had no effect on the retention page | Relationships filter one way (`players → sessions → level_attempts`); a slicer on `sessions` can't filter `players` | Put slicers on the upstream table. Know which way your filters flow |
| Retention looked worse than it was | Players who started recently can't have 7-day retention yet | Added eligibility flags (right-censoring) so they're excluded instead of counted as "didn't return" |
| "Medium" skill players appeared to retain better than "High" | Groups of only ~25–35 players | Treated the gap as noise. **Check sample size before trusting a difference** |
| A donut chart of fuel-starvation rates | Donuts show shares of a whole; these were three separate rates | Column chart. Match the chart type to what the numbers mean |

## Game, web and tooling

| Problem | Cause | Fix / lesson |
|---|---|---|
| First `session_started` event arrived after the first crash | Unity 6's Enter Play Mode settings skipped the initial scene-load callback | Handle the already-active scene in `Start()`, then subscribe to `sceneLoaded` (and unsubscribe in `OnDestroy()`) |
| Game worked in the editor but needed CORS in the browser | Browsers block cross-site requests unless the API allows the origin | Enabled CORS, then locked it to the exact origin itch.io serves games from (`https://html-classic.itch.zone`, not the page URL) |
| WebGL build on itch.io couldn't find `TemplateData/style.css` | Windows PowerShell 5.1's `Compress-Archive` writes backslash paths inside zips, which Linux servers can't resolve | Zip with File Explorer instead |
| Game frame stuck on a grey box, then 504 errors | An ad blocker, then a temporary itch.io server problem | Test in a clean (Incognito) window, and test someone else's game to separate "my build" from "the platform" |
| Dashboard screenshots broken on GitHub | Folder committed as `docs/Images`, README linked `docs/images` | GitHub paths are case-sensitive even though Windows isn't |
| `put-bucket-encryption` rejected valid JSON | Windows PowerShell 5.1 strips inner double quotes when passing arguments to native programs | Pass JSON through a file (`file://…`). S3 encrypts new buckets by default anyway |
| `$default` stage name became empty in a CLI command | PowerShell expanded `$default` as a variable | Single quotes: `'$default'` |

## Security habits that held up

- No AWS credentials in the game client; only a public endpoint behind throttling, validation, CORS and a budget alert.
- Private config (`TelemetryConfig.txt`), the AWS account ID and the API URL kept out of the repo with `.gitignore`, an environment variable for the simulator, and a scan of every committed file before going public.
- The repo was published **private first**, reviewed, then made public.
