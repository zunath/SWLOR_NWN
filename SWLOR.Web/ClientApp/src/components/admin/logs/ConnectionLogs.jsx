import * as React from 'react';
import * as dotnetify from 'dotnetify';
import ReactPaginate from 'react-paginate';

export default class ConnectionLogs extends React.Component {

    constructor(props) {
        super(props);
        this.vm = dotnetify.react.connect('ConnectionLogsViewModel', this);

        this.dispatchState = state => this.vm.$dispatch(state);

        this.state = { PaginatedItems: [], Pages: 1, SelectedPage: 0 }

        this.pageChanged = this.pageChanged.bind(this);
    }

    componentWillUnmount() {
        this.vm.$destroy();
    }

    getTypeOfLogin(eventTypeID) {
        if (eventTypeID === 1)
            return <span className="greenText">Log In</span>;
        else
            return <span className="redText">Log Out</span>;
    }

    pageChanged(page) {
        const pageIndex = page.selected;

        this.setState({
            SelectedPage: pageIndex
        });

        this.dispatchState({ ChangePage: pageIndex });
    }

    render() {
        return (
            <div>

                <div className="row">
                    <table className="table table-responsive">
                        <thead>
                            <tr>
                                <th>Date</th>
                                <th>Event</th>
                                <th>Player</th>
                                <th>CD Key</th>
                                <th>Account</th>
                            </tr>
                        </thead>
                        <tbody>
                            {this.state.PaginatedItems.map(obj => <tr key={obj.ClientLogEventID}>
                                <td>
                                    {obj.DateOfEvent}
                                </td>
                                <td>
                                    {this.getTypeOfLogin(obj.ClientLogEventTypeID)}
                                </td>
                                <td>
                                    {obj.Player === null ? '' : obj.Player.CharacterName}
                                </td>
                                <td>
                                    {obj.CDKey}
                                </td>
                                <td>
                                    {obj.AccountName}
                                </td>
                            </tr>)}
                        </tbody>
                    </table>
                </div>

                <div className="row">
                    <ReactPaginate
                        pageCount={this.state.Pages}
                        pageRangeDisplayed={10}
                        marginPagesDisplayed={3}
                        pageClassName="page-item"
                        nextClassName="page-item"
                        previousClassName="page-item"
                        pageLinkClassName="page-link"
                        nextLinkClassName="page-link"
                        previousLinkClassName="page-link"
                        containerClassName="pagination pagination-lg pull-right"
                        onPageChange={this.pageChanged}
                    >
                    </ReactPaginate>

                </div>
            </div>
        );



    }
}
